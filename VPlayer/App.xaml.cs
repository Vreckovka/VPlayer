﻿using System;
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

      Kernel.BindToSelfInSingletonScope<KeyListener>();

#if DEBUG
      IsConsoleVisible = true;
#endif
    }

    #endregion

    public override void Initialize()
    {
      base.Initialize();

      CvInvoke.Init();
    }

    #region LoadSettings

    private void LoadSettings()
    {
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

  }
}
