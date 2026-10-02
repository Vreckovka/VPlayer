using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using VPlayer.Core.Windows;

namespace VPlayer.Performance
{
  public static class WindowActivationChecks
  {
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint command);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    static void RequireCoveredBy(IntPtr foreground,params Window[] windows)
    {
      foreach(var window in windows)
      {
        var cursor=GetWindow(new WindowInteropHelper(window).Handle,3);
        int checkedWindows=0;
        while(cursor!=IntPtr.Zero && cursor!=foreground && checkedWindows++<10000) cursor=GetWindow(cursor,3);
        Require(cursor==foreground,"An inactive VPlayer window remains above the foreground application: "+window.Title+"; previous handles checked="+checkedWindows+"; topmost="+IsTopmost(window));
      }
    }
    static bool IsTopmost(Window window) => (GetWindowLongPtr(new WindowInteropHelper(window).Handle,-20).ToInt64() & 8)!=0;
    static void Require(bool condition,string message) {if(!condition) throw new InvalidOperationException(message);}

    public static void ShowProbe(string marker,bool topmost=false)
    {
      var app=new Application();
      var window=new Window {Title="VPlayer foreground regression probe",Width=420,Height=220,Left=160,Top=160,Topmost=topmost,
        Content=new TextBlock {Text="This window should remain in front while VPlayer's overlays load.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(20)}};
      EventHandler rendered=null;
      rendered=(sender,args)=>
      {
        window.ContentRendered-=rendered;
        var temporary=marker+".tmp";
        File.WriteAllText(temporary,new WindowInteropHelper(window).Handle.ToInt64().ToString());
        File.Move(temporary,marker);
      };
      window.ContentRendered+=rendered;
      app.Run(window);
    }

    public static void Run(string output,string commit,bool topmostForeground=false)
    {
      output=Path.GetFullPath(output);
      if(File.Exists(output)) throw new IOException("Refusing to overwrite a window regression result.");
      Directory.CreateDirectory(Path.GetDirectoryName(output));
      var marker=output+".foreground-hwnd";
      if(File.Exists(marker) || File.Exists(marker+".tmp")) throw new IOException("Existing foreground probe marker.");
      var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
      Exception failure=null;
      app.Startup+=async (sender,args)=>
      {
        Window host=null,video=null,overlay=null;
        Process probe=null;
        try
        {
          var background=new Border {Background=Brushes.DarkSlateGray};
          host=new Window {Title="VPlayer inactive video host regression",Width=620,Height=400,
            ShowActivated=false,Left=120,Top=120,Content=background};
          int splashClosed=0;
          ShellWindowStartup.Attach(host,()=>splashClosed++);
          host.Show();
          await Task.Delay(50);
          var start=new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName) {UseShellExecute=false,CreateNoWindow=true};
          start.ArgumentList.Add(typeof(Program).Assembly.Location);
          start.ArgumentList.Add("window-foreground-probe");start.ArgumentList.Add(marker);start.ArgumentList.Add(topmostForeground?"topmost":"unused");
          probe=Process.Start(start);
          var deadline=Stopwatch.StartNew();
          while(!File.Exists(marker)) {if(probe.HasExited || deadline.ElapsedMilliseconds>10000) throw new TimeoutException("Foreground probe did not render.");await Task.Delay(25);}
          var foreground=new IntPtr(long.Parse(File.ReadAllText(marker)));
          SetForegroundWindow(foreground);
          await Task.Delay(50);
          Require(GetForegroundWindow()==foreground,"Cannot establish the other application's foreground control.");
          RequireCoveredBy(foreground,host);
          var type=typeof(VVLC.VideoView).Assembly.GetType("VVLC.ForegroundWindow",true);
          video=(Window)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[] {background},null);
          overlay=(Window)type.GetField("overlayWindow",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(video);
          type.GetMethod("Background_Loaded",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(video,new object[] {background,new RoutedEventArgs(FrameworkElement.LoadedEvent,background)});
          await Task.Delay(50);
          Require(GetForegroundWindow()==foreground,"Automatic video/overlay loading stole foreground activation.");
          RequireCoveredBy(foreground,host,video,overlay);
          Require(!IsTopmost(host) && !IsTopmost(video) && !IsTopmost(overlay),"An inactive owned window became topmost.");
          Require(ReferenceEquals(video.Owner,host) && ReferenceEquals(overlay.Owner,video),"Video windows have the wrong ownership.");
          for(int i=0;i<100;i++)
          {
            host.Left=120+i%2;host.Width=620+i%2;
            await (Task)type.GetMethod("SetWindowInPlace",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(video,null);
            Require(GetForegroundWindow()==foreground,"Automatic layout stole foreground activation at iteration "+i);
            RequireCoveredBy(foreground,host,video,overlay);
          }
          for(int i=0;i<10;i++)
          {
            overlay.Hide();video.Hide();
            var show=type.Assembly.GetType("VVLC.OwnedWindowOrder",true).GetMethod("Show",BindingFlags.Static|BindingFlags.NonPublic);
            show.Invoke(null,new object[] {video});show.Invoke(null,new object[] {overlay});await Task.Delay(10);
            Require(GetForegroundWindow()==foreground,"Automatic visibility restoration stole foreground activation.");
            RequireCoveredBy(foreground,host,video,overlay);
          }
          Require(splashClosed==1,"Splash did not close exactly once.");
          // Explicit always-on-top remains authoritative, including while inactive.
          host.Topmost=true;overlay.Hide();video.Hide();
          var showOwned=type.Assembly.GetType("VVLC.OwnedWindowOrder",true).GetMethod("Show",BindingFlags.Static|BindingFlags.NonPublic);
          showOwned.Invoke(null,new object[] {video});showOwned.Invoke(null,new object[] {overlay});
          Require(IsTopmost(host),"The explicit always-on-top setting was overridden.");
          host.Topmost=false;
          SetForegroundWindow(new WindowInteropHelper(host).Handle);await Task.Delay(30);
          Require(GetAncestor(GetForegroundWindow(),3)==new WindowInteropHelper(host).Handle,"Explicit activation did not foreground the host.");
          SetForegroundWindow(foreground);await Task.Delay(30);
          Require(GetForegroundWindow()==foreground,"Another application cannot cover VPlayer after explicit activation.");
          RequireCoveredBy(foreground,host,video,overlay);
          File.WriteAllText(output,JsonSerializer.Serialize(new {Commit=commit,LayoutUpdates=100,VisibilityCycles=10,
            SeparateForegroundProcess=true,ForegroundTopmost=topmostForeground,ZOrderVerified=true,ExplicitActivationVerified=true,ExplicitTopmostPreserved=true,AutomaticActivationChanges=0,TopmostWindows=0,SplashClosures=splashClosed,Status="Passed"},new JsonSerializerOptions {WriteIndented=true}));
        }
        catch(Exception error)
        {
          failure=error;
          File.WriteAllText(output,JsonSerializer.Serialize(new {Commit=commit,Status="Failed",FailureType=error.GetType().FullName,Message=error.Message},new JsonSerializerOptions {WriteIndented=true}));
        }
        finally
        {
          overlay?.Close();video?.Close();host?.Close();
          if(probe!=null) {if(!probe.HasExited){probe.CloseMainWindow();if(!probe.WaitForExit(2000))probe.Kill();}probe.Dispose();}
          if(File.Exists(marker)) File.Delete(marker);
          if(File.Exists(marker+".tmp")) File.Delete(marker+".tmp");
          app.Shutdown();
        }
      };
      app.Run();
      if(failure!=null) throw new InvalidOperationException("Window activation regression failed.",failure);
    }
  }
}
