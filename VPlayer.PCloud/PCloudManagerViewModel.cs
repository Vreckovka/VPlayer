using System;
using System.Collections.Generic;
using System.Text;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.PCloud.ViewModels;
using VPlayer.PCloud.Views;

namespace VPlayer.PCloud
{
  public class PCloudManagerViewModel : RegionViewModel<PCloudManagementView>
  {
    private readonly ICloudService cloudService;

    public PCloudManagerViewModel(
      IRegionProvider regionProvider, 
      ICloudService cloudService,
      PCloudFileBrowserViewModel pCloudFileBrowserViewModel) : base(regionProvider)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));

      PCloudFileBrowserViewModel = pCloudFileBrowserViewModel ?? throw new ArgumentNullException(nameof(pCloudFileBrowserViewModel));
    }


    public PCloudFileBrowserViewModel PCloudFileBrowserViewModel { get; set; }

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

    public override string Header => "Cloud service";

    #region OnActivation

    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        PCloudFileBrowserViewModel.OnBaseDirectoryPathChanged("0");

        //var folders = await cloudService.GetFoldersAsync(0);

        //if (folders == null)
        //{
        //  cloudService.SaveLoginInfo("pecho4@gmail.com", "roman564123a");

        //  folders = await cloudService.GetFoldersAsync(0);
        //}
      }
    }

    #endregion
  }
}
