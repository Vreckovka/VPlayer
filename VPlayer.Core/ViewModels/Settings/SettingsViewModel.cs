using System;
using System.Reflection;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Providers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Views;
using VPlayer.Modularity.NinjectModules;

namespace VPlayer.Core.ViewModels.Settings
{
  public class SettingViewModel : ViewModel
  {

    public SettingViewModel(SettingParameters settingParameters)
    {
      SettingParameters = settingParameters ?? throw new ArgumentNullException(nameof(settingParameters));
    }

    public string Key { get; set; }

    public SettingParameters SettingParameters { get;  }

    public string Value
    {
      get
      {
        return SettingParameters.Value;
      }
      set
      {
        if (value != this.SettingParameters.Value)
        {
          this.SettingParameters.Value = value;
          RaisePropertyChanged();
        }
      }
    }

    public bool CanPickPath => SettingParameters.CanPickPath;
  }

  public class SettingsViewModel : RegionViewModel<SettingsView>, INavigationItem
  {
    private readonly IStorageManager storageManager;
    private readonly ISettingsProvider settingsProvider;

    public SettingsViewModel(IRegionProvider regionProvider,
      IStorageManager storageManager,
      ISettingsProvider settingsProvider,
      IVPlayerInfoProvider iVPlayerInfoProvider) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));

      BuildVersion = iVPlayerInfoProvider.GetApplicationVersion();

      foreach (var setting in settingsProvider.Settings)
      {
        Settings.Add(new SettingViewModel(setting.Value)
        {
          Key = setting.Key,
        });
      }

      Settings.ItemUpdated.Subscribe((x) =>
      {
        var setting = ((SettingViewModel)x.Sender);

        if (setting != null)
          settingsProvider.AddOrUpdateSetting(setting.Key, setting.SettingParameters);
      });
    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public string Header => "Settings";
    public string BuildVersion { get; set; }

    public RxObservableCollection<SettingViewModel> Settings { get; set; } = new RxObservableCollection<SettingViewModel>();

    #region ChoosePath

    private ActionCommand<SettingViewModel> choosePath;

    public ICommand ChoosePath
    {
      get
      {
        if (choosePath == null)
        {
          choosePath = new ActionCommand<SettingViewModel>(OnChoosePath);
        }

        return choosePath;
      }
    }

    public void OnChoosePath(SettingViewModel settingViewModel)
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = true;
      dialog.InitialDirectory = settingViewModel.Value;
      dialog.Title = "Select folder";

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        settingViewModel.Value = dialog.FileName;
      }
    }

    #endregion

    #region DeleteAllData

    private ActionCommand deleteAllData;

    public ICommand DeleteAllData
    {
      get
      {
        if (deleteAllData == null)
        {
          deleteAllData = new ActionCommand(OnDeleteAllData);
        }

        return deleteAllData;
      }
    }

    public async void OnDeleteAllData()
    {
      await storageManager.ClearStorage();
    }

    #endregion 

    #region CleanData

    private ActionCommand cleanData;

    public ICommand CleanData
    {
      get
      {
        if (cleanData == null)
        {
          cleanData = new ActionCommand(OnCleanData);
        }

        return cleanData;
      }
    }

    public async void OnCleanData()
    {
      await storageManager.CleanData();
    }

    #endregion 

    #region DownloadNotDownloaded

    private ActionCommand downloadNotDownloaded;

    public ICommand DownloadNotDownloaded
    {
      get
      {
        if (downloadNotDownloaded == null)
        {
          downloadNotDownloaded = new ActionCommand(OnDownloadNotDownloaded);
        }

        return downloadNotDownloaded;
      }
    }

    public async void OnDownloadNotDownloaded()
    {
      await storageManager.DownloadAllNotYetDownloaded(true);
    }

    #endregion 
  }
}
