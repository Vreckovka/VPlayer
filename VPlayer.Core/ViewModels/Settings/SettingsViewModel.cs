using System;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Providers;
using VPlayer.Core.Views;

namespace VPlayer.Core.ViewModels.Settings
{
  public class SettingsViewModel : RegionViewModel<SettingsView>, INavigationItem
  {
    private readonly IStorageManager storageManager;

    public SettingsViewModel(IRegionProvider regionProvider, IStorageManager storageManager, IBasicInformationProvider basicInformationProvider) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));

      BuildVersion = basicInformationProvider.GetBuildVersion();
    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public string Header => "Settings";

    public string BuildVersion { get; set; }

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
