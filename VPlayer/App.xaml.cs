using System;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using Ninject;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Regions;
using VCore.Modularity.NinjectModules;
using VCore.Other;
using VCore.Standard;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.IPTV.Modularity;
using VPlayer.Modularity.NinjectModules;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.Views;


namespace VPlayer
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  ///

  public partial class App : PrismApplication
  {
    private IKernel Kernel;
    private bool isConsoleUp = false;
    private Stopwatch stopWatch;

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
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

#if DEBUG

      //isConsoleUp = WinConsole.CreateConsole();

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("INITIALIZING");

#endif
    }

    protected override Window CreateShell()
    {
      var shell = Container.Resolve<MainWindow>();

      RegionManager.SetRegionManager(shell, Kernel.Get<IRegionManager>());
      RegionManager.UpdateRegions();

      var dataContext = Container.Resolve<MainWindowViewModel>();
      shell.DataContext = dataContext;

      return shell;
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
      stopWatch.Stop();

      Console.WriteLine(stopWatch.Elapsed);
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
      //moduleCatalog.AddModule<WindowsViewModel>();
      //moduleCatalog.AddModule<LibraryViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
      if (isConsoleUp)
        isConsoleUp = !WinConsole.FreeConsole();

      base.OnExit(e);
    }

    private  Assembly Resolver(object sender, ResolveEventArgs args)
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
  }
}
