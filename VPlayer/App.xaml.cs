using System;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using Ninject;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Regions;
using VCore.Modularity.NinjectModules;
using VCore.Other;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Modularity.NinjectModules;
using VPlayer.ViewModels;
using VPlayer.Views;


namespace VPlayer
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : PrismApplication
  {
    private IKernel Kernel;
    private bool isConsoleUp = false;
    private Stopwatch stopWatch;
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
      stopWatch = new Stopwatch();
      stopWatch.Start();

      var sQLiteProviderFactory = new System.Data.SQLite.EF6.SQLiteProviderFactory();
      var sQLiteFactory = new System.Data.SQLite.SQLiteFactory();


      DbProviderFactories.RegisterFactory("System.Data.SQLite.EF6", sQLiteProviderFactory);
      DbProviderFactories.RegisterFactory("System.Data.SQLite", sQLiteFactory);

      Kernel = Container.GetContainer();

      Kernel.Load<CommonNinjectModule>();
      Kernel.Load<WPFNinjectModule>();
      Kernel.Load<VPlayerNinjectModule>();

#if DEBUG

      isConsoleUp = WinConsole.CreateConsole();

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("TU JE MOJ TEXT");
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
  }
}
