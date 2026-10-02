using System;
using System.Linq;
using System.Reflection;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using Moq;
using PCloudClient;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Player.Behaviors;

namespace VPlayer.TestSupport
{
  public sealed class LyricsAnimationFixture : IDisposable
  {
    private readonly SynchronizationContext previousUi=VSynchronizationContext.UISynchronizationContext;
    private readonly Dispatcher previousDispatcher=VSynchronizationContext.UIDispatcher;
    public ListView View {get;}
    public AutoScrollLyricsBehavior Behavior {get;}=new AutoScrollLyricsBehavior {StepSize=31,AnimationTime=TimeSpan.FromMilliseconds(600)};
    public LyricsAnimationFixture(LRCFileViewModel lyrics)
    {
      VSynchronizationContext.UISynchronizationContext=new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
      VSynchronizationContext.UIDispatcher=Dispatcher.CurrentDispatcher;
      View=new ListView {DataContext=lyrics,Width=720,Height=480};
      View.Template=(ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ListView'><Border><ScrollViewer CanContentScroll='True' HorizontalScrollBarVisibility='Disabled' VerticalScrollBarVisibility='Hidden'><ItemsPresenter /></ScrollViewer></Border></ControlTemplate>");
      View.ItemContainerStyle=new Style(typeof(ListViewItem));
      View.ItemContainerStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty,31.0));
      var actual=new DataTrigger {Binding=new Binding("IsActual"),Value=true};
      actual.Setters.Add(new Setter(Control.ForegroundProperty,System.Windows.Media.Brushes.DeepPink));
      actual.Setters.Add(new Setter(Control.FontWeightProperty,FontWeights.Bold));
      View.ItemContainerStyle.Triggers.Add(actual);
      View.DisplayMemberPath="Text";
      VirtualizingPanel.SetIsVirtualizing(View,true);
      VirtualizingPanel.SetScrollUnit(View,ScrollUnit.Pixel);
      VirtualizingPanel.SetVirtualizationMode(View,VirtualizationMode.Recycling);
      View.SetBinding(ItemsControl.ItemsSourceProperty,new Binding("LinesView"));
      Behavior.Attach(View);
      View.ApplyTemplate();
      Layout();
    }
    public ScrollViewer Scroller=>(ScrollViewer)((Decorator)System.Windows.Media.VisualTreeHelper.GetChild(View,0)).Child;
    public void Layout(){View.Measure(new Size(720,480));View.Arrange(new Rect(0,0,720,480));View.UpdateLayout();}
    public void Load(){View.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));}
    public void Unload(){View.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));}
    public static bool Observed(LRCFileViewModel lyrics)=>((ReplaySubject<int>)typeof(LRCFileViewModel).GetField("actualLineSubject",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lyrics)).HasObservers;
    public static LRCFileViewModel Create(int count)
    {
      var windows=new Mock<IWindowManager>().Object;
      var cloud=new PCloudLyricsProvider(new Mock<IPCloudService>().Object,windows,new Mock<IPCloudProvider>().Object);
      return new LRCFileViewModel(new LRCFile(Enumerable.Range(0,count).Select(i=>new LRCLyricLine {Timestamp=TimeSpan.FromSeconds(i),Text="Lyric line "+i}).ToList()),LRCProviders.Local,cloud,windows);
    }
    public static void Pump(TimeSpan duration)
    {
      var frame=new DispatcherFrame();
      var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=duration};
      timer.Tick+=(sender,args)=>{timer.Stop();frame.Continue=false;};
      timer.Start();Dispatcher.PushFrame(frame);
    }
    public void Dispose()
    {
      Behavior.Detach();
      VSynchronizationContext.UISynchronizationContext=previousUi;
      VSynchronizationContext.UIDispatcher=previousDispatcher;
    }
  }
}