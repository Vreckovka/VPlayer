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
            using (StartupMeasurements.Measure("Application / benchmark first-frame screenshot"))
              CaptureBenchmarkWindow(window, ".png");
            if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_WAIT_FOR_LIBRARY") == "1")
            {
              var playlists = Kernel.Get<VPlayer.Home.ViewModels.SoundItemPlaylistsViewModel>();
              var deadline = Stopwatch.StartNew();
              while (!playlists.LibraryCollection.WasLoaded || playlists.LoadingStatus.IsLoading || !IsPlaylistViewRendered(window))
              {
                if (deadline.Elapsed > TimeSpan.FromSeconds(45))
                  throw new TimeoutException("Initial playlist view did not finish loading.");
                await Task.Delay(25);
              }
              await Dispatcher.InvokeAsync(() => window.UpdateLayout(), System.Windows.Threading.DispatcherPriority.ContextIdle);
              StartupMeasurements.RecordProcessMilestone("Application / initial playlist view ready");
              var playlistList = FindPlaylistList(window);
              StartupMeasurements.RecordObservation("UI / grouped playlist items", playlistList.Items.Count);
              StartupMeasurements.RecordObservation("UI / realized playlist rows", CountPlaylistRows(playlistList));
              CaptureBenchmarkWindow(window, ".ready.png");
              if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_SCROLL_PLAYLISTS") == "1")
                await BenchmarkPlaylistScroll(window, playlistList);
              if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_STATISTICS") == "1")
                await BenchmarkStatisticsView(window);
            }
          }
          catch (Exception exception)
          {
            StartupMeasurements.Fail(exception);
          }
          finally
          {
            StartupMeasurements.Flush();
            if (Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER") == "1")
              Dispatcher.BeginInvoke(new Action(() => Shutdown()));
          }
        };
        window.ContentRendered += rendered;
      }
      return window;
    }
    private async Task BenchmarkStatisticsView(Window window)
    {
      var statistics = Kernel.Get<VPlayer.Home.ViewModels.Statistics.StatisticsViewModel>();
      using (StartupMeasurements.Measure("UI / statistics / load and render"))
      {
        statistics.IsActive = true;
        Kernel.Get<VCore.WPF.Modularity.RegionProviders.IRegionProvider>().ActivateView(statistics.Guid);
        var deadline = Stopwatch.StartNew();
        while (statistics.ItemsView == null || statistics.PlaylistView == null || statistics.LoadingStatus.IsLoading ||
               FindStatisticsView(window) == null || !HasRenderedPlaylistRow(FindStatisticsView(window)))
        {
          if (deadline.Elapsed > TimeSpan.FromSeconds(30))
          {
            CaptureBenchmarkWindow(window, ".statistics-failed.png");
            throw new TimeoutException($"Statistics view did not render: items={statistics.ItemsView?.Count}, playlists={statistics.PlaylistView?.Count}, loading={statistics.LoadingStatus.IsLoading}, visible={FindStatisticsView(window) != null}.");
          }
          await Task.Delay(25);
        }
        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), System.Windows.Threading.DispatcherPriority.ContextIdle);
      }
      StartupMeasurements.RecordObservation("UI / statistics / empty item rows",
        statistics.ItemsView.Concat(statistics.SoundsItemsView).Concat(statistics.VideosItemsView).Count(item => item == null));
      StartupMeasurements.RecordObservation("UI / statistics / empty playlist rows", statistics.PlaylistView.Count(item => item == null));
      CaptureBenchmarkWindow(window, ".statistics.png");
    }
    private static FrameworkElement FindStatisticsView(System.Windows.DependencyObject root)
    {
      if (root is VPlayer.Home.Views.Statistics.StatisticsView view && view.IsVisible) return view;
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var found = FindStatisticsView(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
        if (found != null) return found;
      }
      return null;
    }
    private static async Task BenchmarkPlaylistScroll(Window window, ListView list)
    {
      var target = list.Items.Cast<VPlayer.Home.ViewModels.SongsPlaylistViewModel>().Last(item => item.IsUserCreated);
      using (StartupMeasurements.Measure("UI / grouped playlists / scroll to last favorite"))
      {
        var rows = FindPlaylistRowsList(list, target) ?? list;
        rows.ScrollIntoView(target);
        var deadline = Stopwatch.StartNew();
        ListViewItem row;
        do
        {
          await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), System.Windows.Threading.DispatcherPriority.ContextIdle);
          row = rows.ItemContainerGenerator.ContainerFromItem(target) as ListViewItem;
          row?.BringIntoView();
          if (deadline.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Last favorite row did not enter the viewport.");
          await Task.Delay(16);
        } while (row == null || !IsRowInViewport(row));
      }
      StartupMeasurements.RecordObservation("UI / realized playlist rows after scroll", CountPlaylistRows(list));
      StartupMeasurements.RecordObservation("UI / last favorite visible", 1);
      CaptureBenchmarkWindow(window, ".scrolled.png");
    }
    private static ListView FindPlaylistRowsList(System.Windows.DependencyObject root, object target)
    {
      if (root is VPlayer.Home.Views.GroupedPlaylistListView list && list.Items.Contains(target)) return list;
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var found = FindPlaylistRowsList(System.Windows.Media.VisualTreeHelper.GetChild(root, i), target);
        if (found != null) return found;
      }
      return null;
    }
    private static bool HasRenderedPlaylistRow(System.Windows.DependencyObject root)
    {
      if (root is ListView list && list.Items.Count > 0 &&
          list.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement item && item.ActualHeight > 0) return true;
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        if (HasRenderedPlaylistRow(System.Windows.Media.VisualTreeHelper.GetChild(root, i))) return true;
      return false;
    }
    private static bool IsRowInViewport(FrameworkElement row)
    {
      var parent = System.Windows.Media.VisualTreeHelper.GetParent(row);
      while (parent != null && !(parent is ScrollViewer)) parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
      if (!(parent is ScrollViewer viewer) || row.ActualHeight <= 0) return false;
      var bounds = row.TransformToAncestor(viewer).TransformBounds(new Rect(row.RenderSize));
      return bounds.Top >= 0 && bounds.Bottom <= viewer.ActualHeight && bounds.Right > 0 && bounds.Left < viewer.ActualWidth;
    }
    private static ListView FindPlaylistList(System.Windows.DependencyObject root)
    {
      if (root is ListView list && list.Name == "playlists" && list.IsVisible) return list;
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
      {
        var found = FindPlaylistList(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
        if (found != null) return found;
      }
      return null;
    }
    private static int CountPlaylistRows(System.Windows.DependencyObject root)
    {
      int count = root is ListViewItem ? 1 : 0;
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        count += CountPlaylistRows(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
      return count;
    }
    private static bool IsPlaylistViewRendered(System.Windows.DependencyObject root)
    {
      if (root is System.Windows.Controls.ListView list && list.Name == "playlists" && list.IsVisible)
        return list.Items.Count > 0 && HasRenderedPlaylistRow(list);
      for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        if (IsPlaylistViewRendered(System.Windows.Media.VisualTreeHelper.GetChild(root, i))) return true;
      return false;
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
