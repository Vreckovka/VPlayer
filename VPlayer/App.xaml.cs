using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
      base.LoadModules();

      Kernel.Load<VPlayerNinjectModule>();

      Kernel.Rebind<IWindowManager>().To<VPlayerWindowManager>();
    }

    #endregion

    #region LoadSettings

    private void LoadSettings()
    {
      var provider = Container.Resolve<ISettingsProvider>();

      var wasLoaded = provider.Load();

      if (!wasLoaded)
      {
        provider.AddOrUpdateSetting(nameof(GlobalSettings.CloudBrowserInitialDirectory), new SettingParameters("0"));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.FileBrowserInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.MusicInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
        provider.AddOrUpdateSetting(nameof(GlobalSettings.TvShowInitialDirectory), new SettingParameters(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), true));
      }
    }

    #endregion
    
    #region OnContainerCreated

    protected override void OnContainerCreated()
    {
      base.OnContainerCreated();


      LoadSettings();
    }

    #endregion
    
    #region OnUnhandledExceptionCaught

    private IStatusManager statusManager;
    protected override void OnUnhandledExceptionCaught(Exception exception)
    {
      base.OnUnhandledExceptionCaught(exception);

      Application.Current?.Dispatcher?.Invoke(() =>
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
            Message = "Error occured: " + exception.ToString()
          });
        }
      });
    }

    #endregion
  }

  public partial class App : VPlayerApplication
  {
   
  }
}
