using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Views;

namespace VPlayer.Core.ViewModels.Settings
{
  public class SettingsViewModel : RegionViewModel<SettingsView>, INavigationItem
  {
    private readonly IStorageManager storageManager;

    public SettingsViewModel(IRegionProvider regionProvider, IStorageManager storageManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public string Header => "Settings";

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

    #endregion NextSong
  }
}
