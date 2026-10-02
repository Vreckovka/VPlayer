using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;
using VCore.WPF;
using VCore.WPF.Behaviors;
using VCore.WPF.Helpers;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;

namespace VPlayer.Player.Behaviors
{
  public class AutoScrollLyricsBehavior : Behavior<ListView>
  {
    public static readonly DependencyProperty StepSizeProperty=DependencyProperty.Register(
      nameof(StepSize),typeof(double),typeof(AutoScrollLyricsBehavior),new PropertyMetadata(-1.0));
    public double StepSize {get=>(double)GetValue(StepSizeProperty);set=>SetValue(StepSizeProperty,value);}
    public TimeSpan AnimationTime {get;set;}=TimeSpan.FromSeconds(1);
    private SerialDisposable subscription=new SerialDisposable();
    private ScrollViewer scrollViewer;
    private bool loaded,wasUnloaded;
    private int subscriptionGeneration,animationGeneration;

    protected override void OnAttached()
    {
      base.OnAttached();
      subscription.Dispose();subscription=new SerialDisposable();
      AssociatedObject.Loaded+=OnLoaded;
      AssociatedObject.Unloaded+=OnUnloaded;
      AssociatedObject.DataContextChanged+=OnDataContextChanged;
      if(AssociatedObject.IsLoaded)OnLoaded(AssociatedObject,new RoutedEventArgs());
    }
    protected override void OnDetaching()
    {
      if(AssociatedObject!=null)
      {
        AssociatedObject.Loaded-=OnLoaded;
        AssociatedObject.Unloaded-=OnUnloaded;
        AssociatedObject.DataContextChanged-=OnDataContextChanged;
      }
      OnUnloaded(null,null);subscription.Dispose();
      base.OnDetaching();
    }
    private void OnLoaded(object sender,RoutedEventArgs args)
    {
      loaded=true;SubscribeToCurrentLyrics(false);
    }
    private void OnUnloaded(object sender,RoutedEventArgs args)
    {
      loaded=false;subscriptionGeneration++;subscription.Disposable=null;
      StopAnimation();scrollViewer=null;wasUnloaded=true;
    }
    private void OnDataContextChanged(object sender,DependencyPropertyChangedEventArgs args)=>SubscribeToCurrentLyrics(true);
    private void SubscribeToCurrentLyrics(bool resetTop)
    {
      var generation=++subscriptionGeneration;
      subscription.Disposable=null;StopAnimation();
      if(!loaded || AssociatedObject==null)return;
      var owner=AssociatedObject.DataContext;
      if(resetTop)AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action(()=>
      {
        if(IsCurrent(generation,owner))GetScrollViewer()?.ScrollToTop();
      }));
      if(owner is LRCFileViewModel lyrics)
        subscription.Disposable=lyrics.ActualLineChanged.ObserveOnDispatcher().Subscribe(index=>
        {if(IsCurrent(generation,owner))OnLineChanged(index);});
      else if(owner is LRCCreatorViewModel creator)
        subscription.Disposable=creator.ObservePropertyChange(x=>x.ActualLine).ObserveOnDispatcher().Subscribe(line=>
        {if(line!=null && IsCurrent(generation,owner))OnLineChanged(creator.Lines.IndexOf(line));});
    }
    private bool IsCurrent(int generation,object owner)=>loaded && AssociatedObject!=null &&
      generation==subscriptionGeneration && ReferenceEquals(AssociatedObject.DataContext,owner);
    private ScrollViewer GetScrollViewer()
    {
      if(scrollViewer==null && AssociatedObject!=null && VisualTreeHelper.GetChildrenCount(AssociatedObject)>0)
        scrollViewer=(VisualTreeHelper.GetChild(AssociatedObject,0) as Decorator)?.Child as ScrollViewer;
      return scrollViewer;
    }
    private void StopAnimation()
    {
      animationGeneration++;
      if(scrollViewer==null)return;
      var offset=scrollViewer.VerticalOffset;
      scrollViewer.BeginAnimation(ScrollAnimationBehavior.VerticalOffsetProperty,null);
      scrollViewer.SetCurrentValue(ScrollAnimationBehavior.VerticalOffsetProperty,offset);
    }
    private void OnLineChanged(int lineIndex)
    {
      if(!loaded || StepSize<=0 || double.IsNaN(StepSize) || double.IsInfinity(StepSize))return;
      var viewer=GetScrollViewer();if(viewer==null)return;
      var target=Math.Min(viewer.ScrollableHeight,Math.Max(0,(lineIndex<=1?0:lineIndex-1)*StepSize));
      var from=viewer.VerticalOffset;
      var jump=wasUnloaded || Math.Abs(from-target)>StepSize*10 || AnimationTime<=TimeSpan.Zero;
      wasUnloaded=false;StopAnimation();
      if(jump || Math.Abs(from-target)<0.1)
      {
        viewer.SetCurrentValue(ScrollAnimationBehavior.VerticalOffsetProperty,target);
        viewer.ScrollToVerticalOffset(target);return;
      }
      var generation=animationGeneration;
      var animation=new DoubleAnimation(from,target,new Duration(AnimationTime))
      {SpeedRatio=0.95,AccelerationRatio=0.2,DecelerationRatio=0.8,FillBehavior=FillBehavior.Stop};
      animation.Completed+=(sender,args)=>
      {
        if(loaded && generation==animationGeneration && ReferenceEquals(scrollViewer,viewer))
        {
          viewer.SetCurrentValue(ScrollAnimationBehavior.VerticalOffsetProperty,target);
          viewer.ScrollToVerticalOffset(target);
        }
      };
      viewer.BeginAnimation(ScrollAnimationBehavior.VerticalOffsetProperty,animation,HandoffBehavior.SnapshotAndReplace);
    }
  }
}