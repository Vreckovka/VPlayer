using System;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Logger;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Regions;
using VCore.Modularity.NinjectModules;
using VCore.Other;
using VCore.Standard;
using VCore.Standard.Modularity.NinjectModules;
using VCore.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.Windows;
using VCore.WPF.Views;
using VPlayer.IPTV.Modularity;
using VPlayer.Modularity.NinjectModules;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.Views;
using SplashScreen = System.Windows.SplashScreen;


namespace VPlayer
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  ///

  public abstract class VApplication : PrismApplication
  {
    protected void ShowSplashScreen()
    {
      var thread = new Thread(() =>
      {
        var splashScreen = new SplashScreenWindow();
        splashScreen.DataContext = new SplashScreenViewModel();

        splashScreen.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        splashScreen.WindowStyle = WindowStyle.None;
        splashScreen.AllowsTransparency = true;
        splashScreen.ResizeMode = ResizeMode.NoResize;
        splashScreen.ShowInTaskbar = true;

        splashScreen.Content = new VPlayerSplashScreen();

        if (splashScreen != null)
        {
          splashScreen.Show();

          EventHandler closedEventHandler = null;

          closedEventHandler = (o, s) =>
          {
            splashScreen.Closed -= closedEventHandler;
            splashScreen.Dispatcher.InvokeShutdown();
          };

          splashScreen.Closed += closedEventHandler;

          System.Windows.Threading.Dispatcher.Run();
        }
      });

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
    }
  }

  public partial class App : VApplication
  {
    private IKernel Kernel;
    private bool isConsoleUp = false;
    private Stopwatch stopWatch;
    private ILogger logger;
    private IWindowManager windowManager;

    #region BuildVersion

    public static string BuildVersion { get; set; }

    #endregion

    #region RegisterTypes


    protected override async void RegisterTypes(IContainerRegistry containerRegistry)
    {
      stopWatch = new Stopwatch();
      stopWatch.Start();

      Kernel = Container.GetContainer();

      AppDomain.CurrentDomain.AssemblyResolve += Resolver;

      VIoc.Kernel = Kernel;

      Kernel.Load<CommonNinjectModule>();
      Kernel.Load<WPFNinjectModule>();
      Kernel.Load<VPlayerNinjectModule>();

      CultureInfo.CurrentCulture = new CultureInfo("en-US");

      logger = Container.Resolve<ILogger>();
      windowManager = Container.Resolve<IWindowManager>();

      ShowSplashScreen();

#if DEBUG

      isConsoleUp = WinConsole.CreateConsole();

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("INITIALIZING");

#endif

      Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

      DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

      BuildVersion = $"{version} ({buildDate.ToString("dd.MM.yyyy")})";
    }

    #endregion

    private MainWindow CreateMainWindow()
    {
      var shell = Container.Resolve<MainWindow>();

      RegionManager.SetRegionManager(shell, Kernel.Get<IRegionManager>());

      RegionManager.UpdateRegions();

      var dataContext = Kernel.Get<MainWindowViewModel>();

      shell.DataContext = dataContext;

      return shell;
    }

   

    #region CreateShell


    protected override Window CreateShell()
    {
      return CreateMainWindow();
    }

    #endregion

    protected override async void OnInitialized()
    {
      base.OnInitialized();

      SetupExceptionHandling();

      stopWatch.Stop();

      Console.WriteLine(stopWatch.Elapsed);

    }

    protected override void OnExit(ExitEventArgs e)
    {
      if (isConsoleUp)
        isConsoleUp = !WinConsole.FreeConsole();

      base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      Control.IsTabStopProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(false));

      base.OnStartup(e);
    }

    private Assembly Resolver(object sender, ResolveEventArgs args)
    {
      if (args.Name.StartsWith("CefSharp.Core.Runtime"))
      {
        string assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
        string archSpecificPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
          Environment.Is64BitProcess ? "x64" : "x86",
          assemblyName);

        return File.Exists(archSpecificPath)
          ? Assembly.LoadFile(archSpecificPath)
          : null;
      }

      return null;
    }

    #region SetupExceptionHandling

    private void SetupExceptionHandling()
    {
      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

      DispatcherUnhandledException += (s, e) =>
      {
        LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        e.Handled = true;
      };

      TaskScheduler.UnobservedTaskException += (s, e) =>
      {
        LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
      };
    }

    #endregion

    #region LogUnhandledException

    private async void LogUnhandledException(Exception exception, string source)
    {
      string message = $"Unhandled exception ({source})";

      try
      {
        System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();

        message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
      finally
      {
        logger.Log(MessageType.Error, message);
        logger.Log(exception);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          windowManager.ShowErrorPrompt(exception);
        });
      }
    }

    #endregion

  }
}
