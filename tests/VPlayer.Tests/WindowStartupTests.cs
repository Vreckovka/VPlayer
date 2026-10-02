using System;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using VCore.WPF.ViewModels;
using VPlayer.Core.Windows;
using Xunit;

namespace VPlayer.Tests
{
  public class WindowStartupTests
  {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShellLoadingPreservesTopmostBindingAndClosesSplashOnce(bool topmost) => Sta.Run(() =>
    {
      var model = new BaseWindowViewModel {TopMost=topmost};
      var shell = new Window {DataContext=model};
      shell.SetBinding(Window.TopmostProperty,new Binding(nameof(model.TopMost)));
      int closed=0;
      ShellWindowStartup.Attach(shell,()=>closed++);
      for(int i=0;i<100;i++) shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent,shell));
      Assert.Equal(1,closed);
      Assert.Equal(topmost,shell.Topmost);
      Assert.True(BindingOperations.IsDataBound(shell,Window.TopmostProperty));
      model.TopMost=!topmost;
      Assert.Equal(!topmost,shell.Topmost);
      shell.Close();
    });

    [Fact]
    public void DescendantLoadingDoesNotCompleteShellStartup() => Sta.Run(() =>
    {
      var child = new FrameworkElement();
      var shell = new Window {Content=child};
      int closed=0;
      ShellWindowStartup.Attach(shell,()=>closed++);
      shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent,child));
      Assert.Equal(0,closed);
      shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent,shell));
      Assert.Equal(1,closed);
      shell.Close();
    });

    [Fact]
    public void VideoSurfaceDoesNotShowBeforeItsOwnerIsAttached() => Sta.Run(() =>
    {
      var window=new Window();
      var show=typeof(VVLC.VideoView).Assembly.GetType("VVLC.OwnedWindowOrder",true)
        .GetMethod("Show",BindingFlags.Static|BindingFlags.NonPublic);
      try {show.Invoke(null,new object[] {window});Assert.False(window.IsVisible);}
      finally {window.Close();}
    });

    [Fact]
    public void VideoAndControlsDoNotActivateOnAutomaticShow() => Sta.Run(() =>
    {
      var type=typeof(VVLC.VideoView).Assembly.GetType("VVLC.ForegroundWindow",true);
      var video=(Window)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[] {new FrameworkElement()},null);
      var overlay=(Window)type.GetField("overlayWindow",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(video);
      try
      {
        Assert.False(video.ShowActivated);
        Assert.False(overlay.ShowActivated);
        Assert.False(video.Topmost);
        Assert.False(overlay.Topmost);
      }
      finally {overlay.Close();video.Close();}
    });
  }
}
