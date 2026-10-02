using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Ninject;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Home.ViewModels;
using VPlayer.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;
using VPLayer.Domain.Diagnostics;

namespace VPlayer
{
  public partial class VPlayerApplication
  {
    private async Task BenchmarkMusicPlaylist(Window window)
    {
      var shell=(MainWindowViewModel)window.DataContext;
      var windows=(WindowsViewModel)shell.NavigationViewModel.Items.OfType<NavigationItem>().Single(item=>item.Model is WindowsViewModel).Model;
      var navigation=windows.NavigationViewModel.Items.OfType<NavigationItem>().Single(item=>item.Model is MusicPlayerViewModel);
      var player=(MusicPlayerViewModel)navigation.Model;
      using(StartupMeasurements.Measure("UI / music playlist / activation and render"))
      {
        navigation.IsActive=true;
        await WaitForMusic(window,()=>player.PlayList.Count>0 && FindTrackList(window)!=null &&
          FindTrackList(window).ItemContainerGenerator.ContainerFromIndex(0) is ListViewItem,"Initial music view",20);
      }
      await player.ClearPlaylist();
      var playlists=Kernel.Get<SoundItemPlaylistsViewModel>();
      var saved=playlists.LibraryCollection.Items.Single(item=>item.Model.Name=="VPlayer benchmark 100000");
      SoundItemInPlaylistViewModel[] incoming;
      using(StartupMeasurements.Measure("UI / music playlist / load and render"))
      {
        using(StartupMeasurements.Measure("UI / music playlist / read and create incoming views"))
          incoming=await Task.Run(async ()=>(await saved.GetItemsToPlay()).ToArray());
        if(incoming.Length!=100000) throw new InvalidOperationException("Large playlist did not load every occurrence.");
        saved.PublishPlayEvent(incoming,EventAction.InitSetPlaylist);
        await WaitForMusic(window,()=>player.ActualSavedPlaylist.Id==saved.Model.Id && player.PlayList.Count==100000 &&
          player.VirtualizedPlayList?.Count==100000 && MusicRowsReady(window,player),"100k music playlist",40);
      }
      if(!incoming.Select(item=>item.Model.Id).SequenceEqual(player.PlayList.Select(item=>item.Model.Id)))
        throw new InvalidOperationException("Rendered playlist changed occurrence order.");
      StartupMeasurements.RecordObservation("UI / music playlist / ordered occurrence check",1);
      StartupMeasurements.RecordObservation("UI / music playlist / items",player.PlayList.Count);
      StartupMeasurements.RecordObservation("UI / music playlist / realized rows",CountTrackRows(FindTrackList(window)));
      CaptureBenchmarkWindow(window,".music-playlist.png");
      var list=FindTrackList(window);
      try
      {
        using(StartupMeasurements.Measure("UI / music playlist / scroll to last track"))
        {
          var viewer=FindMusicScroll(list);
          if(viewer==null) throw new InvalidOperationException("Music playlist has no scroll viewer.");
          viewer.ScrollToEnd();
          await WaitForMusic(window,()=>list.ItemContainerGenerator.ContainerFromIndex(99999) is ListViewItem row &&
            row.DataContext is SoundItemInPlaylistViewModel item && ReferenceEquals(item,player.PlayList[99999]) && IsRowInViewport(row),
            "Last music track",10);
        }
      }
      catch
      {
        var viewer=FindMusicScroll(list);
        StartupMeasurements.RecordObservation("UI / music playlist / failed scroll offset",(long)(viewer?.VerticalOffset ?? -1));
        StartupMeasurements.RecordObservation("UI / music playlist / failed scroll extent",(long)(viewer?.ExtentHeight ?? -1));
        StartupMeasurements.RecordObservation("UI / music playlist / failed scroll viewport",(long)(viewer?.ViewportHeight ?? -1));
        StartupMeasurements.RecordObservation("UI / music playlist / failed scroll last realized index",LastMusicRowIndex(list,list));
        if(list.ItemContainerGenerator.ContainerFromIndex(99999) is ListViewItem row)
        {
          StartupMeasurements.RecordObservation("UI / music playlist / failed scroll final model matches",ReferenceEquals(row.DataContext,player.PlayList[99999])?1:0);
          if(viewer!=null)
          {
            var bounds=row.TransformToAncestor(viewer).TransformBounds(new Rect(row.RenderSize));
            StartupMeasurements.RecordObservation("UI / music playlist / failed scroll final top",(long)(bounds.Top*1000));
            StartupMeasurements.RecordObservation("UI / music playlist / failed scroll final bottom",(long)(bounds.Bottom*1000));
          }
        }
        CaptureBenchmarkWindow(window,".music-scroll-failed.png");
        throw;
      }
      StartupMeasurements.RecordObservation("UI / music playlist / last track visible",1);
      StartupMeasurements.RecordObservation("UI / music playlist / realized rows after scroll",CountTrackRows(list));
      CaptureBenchmarkWindow(window,".music-scroll.png");
      var longest=incoming.Where(item=>!string.IsNullOrEmpty(item.Name)).OrderByDescending(item=>item.Name.Length).First().Name;
      var near=longest.Substring(0,longest.Length/2)+"¤"+longest.Substring(longest.Length/2+1);
      foreach(var query in new[] {new {Name="long no-match",Text=new string('¤',longest.Length)},new {Name="long near-match",Text=near}})
      {
        var previous=player.VirtualizedPlayList;
        using(StartupMeasurements.Measure("UI / music playlist / "+query.Name+" search and render"))
        {
          player.ActualSearch=query.Text;
          await WaitForMusic(window,()=>!ReferenceEquals(previous,player.VirtualizedPlayList) &&
            MusicRowsReady(window,player),query.Name+" music search",20);
        }
        var ids=player.VirtualizedPlayList.Generator.AllItems.Select(item=>item.Model.Id).ToArray();
        using var hashing=SHA256.Create();
        var hash=hashing.ComputeHash(Encoding.UTF8.GetBytes(string.Join(",",ids)));
        StartupMeasurements.RecordObservation("UI / music playlist / "+query.Name+" matches",ids.Length);
        StartupMeasurements.RecordObservation("UI / music playlist / "+query.Name+" ordered ids hash",BitConverter.ToInt64(hash,0));
        StartupMeasurements.RecordObservation("UI / music playlist / "+query.Name+" query length",query.Text.Length);
        CaptureBenchmarkWindow(window,".music-"+query.Name.Replace(' ','-')+".png");
      }
    }
    private static bool MusicRowsReady(Window window,MusicPlayerViewModel player)
    {
      var list=FindTrackList(window);
      if(list==null || player.VirtualizedPlayList==null || !ReferenceEquals(list.ItemsSource,player.VirtualizedPlayList) ||
         list.Items.Count!=player.VirtualizedPlayList.Count) return false;
      return list.Items.Count==0 || (list.ItemContainerGenerator.ContainerFromIndex(0) is ListViewItem row &&
        row.ActualHeight>0 && row.DataContext is SoundItemInPlaylistViewModel item && ReferenceEquals(item,player.VirtualizedPlayList[0]));
    }
    private static async Task WaitForMusic(Window window,Func<bool> ready,string name,int seconds)
    {
      var deadline=Stopwatch.StartNew();
      while(true)
      {
        await window.Dispatcher.InvokeAsync(()=>window.UpdateLayout(),DispatcherPriority.ContextIdle);
        if(ready())
        {
          await WaitForMusicFrame(window,seconds);
          if(ready())
          {
            StartupMeasurements.RecordObservation("UI / music playlist / painted "+name,1);
            return;
          }
        }
        if(deadline.Elapsed>TimeSpan.FromSeconds(seconds)) throw new TimeoutException(name+" did not finish rendering.");
        await Task.Delay(25);
      }
    }
    private static async Task WaitForMusicFrame(Window window,int seconds)
    {
      var rendered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      EventHandler frame=(sender,args)=>rendered.TrySetResult(true);
      CompositionTarget.Rendering+=frame;
      try
      {
        window.InvalidateVisual();
        await Task.WhenAny(rendered.Task,Task.Delay(TimeSpan.FromSeconds(seconds)));
        if(!rendered.Task.IsCompleted) throw new TimeoutException("Music view did not paint a frame.");
        await window.Dispatcher.InvokeAsync(()=>window.UpdateLayout(),DispatcherPriority.ContextIdle);
      }
      finally
      {
        CompositionTarget.Rendering-=frame;
      }
    }
    private static ListView FindTrackList(DependencyObject root)
    {
      if(root is ListView list && list.Name=="ListView_Tracks" && list.IsVisible) return list;
      for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
      {
        var found=FindTrackList(VisualTreeHelper.GetChild(root,i));
        if(found!=null) return found;
      }
      return null;
    }
    private static ScrollViewer FindMusicScroll(DependencyObject root)
    {
      if(root is ScrollViewer viewer) return viewer;
      for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
      {
        var found=FindMusicScroll(VisualTreeHelper.GetChild(root,i));
        if(found!=null) return found;
      }
      return null;
    }
    private static int LastMusicRowIndex(ListView list,DependencyObject root)
    {
      int last=root is ListViewItem row?list.ItemContainerGenerator.IndexFromContainer(row):-1;
      for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        last=Math.Max(last,LastMusicRowIndex(list,VisualTreeHelper.GetChild(root,i)));
      return last;
    }
    private static int CountTrackRows(DependencyObject root)
    {
      int count=root is ListViewItem row && row.DataContext is SoundItemInPlaylistViewModel && row.IsVisible && row.ActualHeight>0?1:0;
      for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) count+=CountTrackRows(VisualTreeHelper.GetChild(root,i));
      return count;
    }
  }
}
