using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChromeDriverScrapper;
using Emgu.CV;
using Listener;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Prism.Ioc;
using Prism.Modularity;
using VCore.Standard.Modularity.NinjectModules;
using VCore.Standard.Providers;
using VCore.WPF;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.Windows;
using VCore.WPF.Views;
using VCore.WPF.Views.SplashScreen;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.Core;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.IPTV.Modularity;
using VPlayer.Modularity.NinjectModules;
using VPlayer.Providers;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.Views;
using VPLayer.Domain.Diagnostics;


namespace VPlayer
{
  public class VPlayerApplication : VApplication<MainWindow, MainWindowViewModel, VPlayerSplashScreen>
  {

    protected override void ShowConsole()
    {
      //IsConsoleVisible = true;
      base.ShowConsole();
    }

    #region LoadModules

    protected override void LoadModules()
    {
      using var measurement = StartupMeasurements.Measure("Application / module registration");
      base.LoadModules();

      Kernel.Load<VPlayerNinjectModule>();

      var benchmarkDirectory = Environment.GetEnvironmentVariable("VPLAYER_BENCHMARK_DIRECTORY");
      if (!string.IsNullOrWhiteSpace(benchmarkDirectory))
        Kernel.Rebind<ISettingsProvider>().To<SettingsProvider>().InSingletonScope()
          .WithConstructorArgument("settingsPath", Path.Combine(Path.GetFullPath(benchmarkDirectory), "settings", "settings.txt"));

      Kernel.Rebind<IWindowManager>().To<VPlayerWindowManager>();

      Kernel.BindToSelfInSingletonScope<KeyListener>();

#if DEBUG
      IsConsoleVisible = true;
#endif
    }

    #endregion

    public override void Initialize()
    {
      using var measurement = StartupMeasurements.Measure("Application / initialization");
      base.Initialize();

      using (StartupMeasurements.Measure("Application / OpenCV native initialization")) CvInvoke.Init();
    }

    protected override void OnInitialized()
    {
      using var measurement = StartupMeasurements.Measure("Application / show shell");
      base.OnInitialized();
    }
    protected override Window CreateShell()
    {
      using var measurement = StartupMeasurements.Measure("Application / shell construction");
      var window = base.CreateShell();
      if (StartupMeasurements.Enabled)
      {
        EventHandler rendered = null;
        rendered = async (sender, args) =>
        {
          window.ContentRendered -= rendered;
          try
          {
            StartupMeasurements.Complete();
            CaptureBenchmarkWindow(window, ".png");
            if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_WAIT_FOR_LIBRARY") == "1")
            {
              var playlists = Kernel.Get<VPlayer.Home.ViewModels.SoundItemPlaylistsViewModel>();
              var deadline = Stopwatch.StartNew();
              while (!playlists.LibraryCollection.WasLoaded || playlists.LoadingStatus.IsLoading)
              {
                if (deadline.Elapsed > TimeSpan.FromSeconds(45))
                  throw new TimeoutException("Initial playlist view did not finish loading.");
                await Task.Delay(25);
              }
              await Dispatcher.InvokeAsync(() => window.UpdateLayout(), System.Windows.Threading.DispatcherPriority.ContextIdle);
              StartupMeasurements.RecordProcessMilestone("Application / initial playlist view ready");
              CaptureBenchmarkWindow(window, ".ready.png");
            }
          }
          catch (Exception exception)
          {
            StartupMeasurements.Fail(exception);
          }
          finally
          {
            if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER") == "1")
              Dispatcher.BeginInvoke(new Action(() => Shutdown()));
          }
        };
        window.ContentRendered += rendered;
      }
      return window;
    }
    private static void CaptureBenchmarkWindow(Window window, string suffix)
    {
      var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
      var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
        (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX),
        (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY),
        96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, System.Windows.Media.PixelFormats.Pbgra32);
      bitmap.Render(window);
      var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
      encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
      using (var stream = File.Create(Path.GetFullPath(Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_RUN_FILE")) + suffix))
        encoder.Save(stream);
    }
    #region LoadSettings

    private void LoadSettings()
    {
      using var measurement = StartupMeasurements.Measure("Application / settings");
      var provider = Container.Resolve<ISettingsProvider>();

      var settings = new Dictionary<string, SettingParameters>()
      {
        { nameof(GlobalSettings.CloudBrowserInitialDirectory), new SettingParameters("0") },
        { nameof(GlobalSettings.MaxItemsForDefaultPlaylist), new SettingParameters("500") },
        { nameof(GlobalSettings.FileBrowserInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true) },
        { nameof(GlobalSettings.MusicInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true) },
        { nameof(GlobalSettings.TvShowInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true) },
      };


      provider.Load();
      var missingSettings = settings.Where(x => !provider.Settings.ContainsKey(x.Key));

      foreach(var missingSetting in missingSettings)
      {
        provider.AddOrUpdateSetting(missingSetting.Key, missingSetting.Value);
      }
    }

    #endregion

    #region OnContainerCreated

    protected override void OnContainerCreated()
    {
      using var measurement = StartupMeasurements.Measure("Application / container activation");
      base.OnContainerCreated();

      var keyListener = Container.Resolve<KeyListener>();

      keyListener.HookKeyboard();

      LoadSettings();
    }

    #endregion

    #region OnUnhandledExceptionCaught

    private IStatusManager statusManager;
    protected override void OnUnhandledExceptionCaught(Exception exception)
    {
      StartupMeasurements.Fail(exception);
      if (StartupMeasurements.Enabled)
      {
        Dispatcher.BeginInvoke(new Action(() => Shutdown(1)));
        return;
      }
      base.OnUnhandledExceptionCaught(exception);

      VSynchronizationContext.PostOnUIThread(() =>
      {
        if (statusManager == null)
        {
          statusManager = Kernel.Get<IStatusManager>();
        }

        if (statusManager != null &&
            statusManager.ActualMessageViewModel != null &&
            (statusManager.ActualMessageViewModel.Status != StatusType.Done ||
             statusManager.ActualMessageViewModel.Status != StatusType.Failed))
        {
          statusManager.UpdateMessage(new StatusMessageViewModel(1)
          {
            Status = StatusType.Error,
            Message = "Error occured: " + exception
          });
        }
      });
    }

    #endregion

    protected override void OnExit(ExitEventArgs e)
    {
      Task.Run(() =>
      {
        Kernel.TryGet<IChromeDriverProvider>()?.ChromeDriver?.Close();
      });


      base.OnExit(e);
    }
  }

  public partial class App : VPlayerApplication
  {
    public App()
    {
      if (StartupMeasurements.Enabled)
      {
        StartupMeasurements.Start();
        DispatcherUnhandledException += (sender, args) =>
        {
          StartupMeasurements.Fail(args.Exception);
          args.Handled = true;
          Dispatcher.BeginInvoke(new Action(() => Shutdown(1)));
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
          if (args.ExceptionObject is Exception exception) StartupMeasurements.Fail(exception);
        };
      }
    }

  }
}
