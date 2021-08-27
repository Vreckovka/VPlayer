using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Prism.Ioc;
using Prism.Modularity;
using VCore.Standard.NewFolder;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.Windows;
using VCore.WPF.Views;
using VCore.WPF.Views.SplashScreen;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.Core;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.IPTV.Modularity;
using VPlayer.Modularity.NinjectModules;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.Views;


namespace VPlayer
{
  public class VPlayerApplication : VApplication<MainWindow, MainWindowViewModel, SplashScreenView>
  {
    protected override void LoadModules()
    {
      base.LoadModules();

      Kernel.Load<VPlayerNinjectModule>();
    }

    private void LoadSettings()
    {
      var provider = Container.Resolve<ISettingsProvider>();

      var wasLoaded = provider.Load();

      if (!wasLoaded)
      {
        provider.AddOrUpdateSetting(nameof(GlobalSettings.CloudBrowserInitialDirectory), new SettingParameters("0"));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.FileBrowserInitialDirectory),  new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.MusicInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.TvShowInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
      }
    }

    private async Task TryMigrateDatabaseAsync()
    {
      logger = Container.Resolve<ILogger>();

      logger.Log(MessageType.Inform, "Migrating database");

      var context = new AudioDatabaseContext();

      await context.Database.MigrateAsync();
    }

    #region OnContainerCreated

    protected override async void OnContainerCreated()
    {
      base.OnContainerCreated();

      Kernel.Load<VPlayerLoggerModule>();

      SplashScreenManager.SetText("Loading settings");

      LoadSettings();

      SplashScreenManager.AddProgress(5);

      SplashScreenManager.SetText("Migrating database");

      await TryMigrateDatabaseAsync();

      SplashScreenManager.AddProgress(10);
    }

    #endregion

    #region RegisterTypes

    protected override async void RegisterTypes(IContainerRegistry containerRegistry)
    {
      base.RegisterTypes(containerRegistry);

      AppDomain.CurrentDomain.AssemblyResolve += Resolver;

    }

    #endregion

    #region Resolver

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

    #endregion
  }

  public partial class App : VPlayerApplication
  {


  }
}
