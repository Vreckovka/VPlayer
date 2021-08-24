using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.WPF.Managers;
using VCore.WPF.Prompts;
using VPlayer.Core;
using VPlayer.Core.Modularity.Regions;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.PCloud.ViewModels;
using VPlayer.PCloud.Views;

namespace VPlayer.PCloud
{
  public class PCloudManagerViewModel : RegionViewModel<PCloudManagementView>
  {
    private readonly ICloudService cloudService;
    private readonly IWindowManager windowManager;

    public PCloudManagerViewModel(
      IRegionProvider regionProvider,
      ICloudService cloudService,
      IWindowManager windowManager,
      PCloudFileBrowserViewModel pCloudFileBrowserViewModel) : base(regionProvider)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      PCloudFileBrowserViewModel = pCloudFileBrowserViewModel ?? throw new ArgumentNullException(nameof(pCloudFileBrowserViewModel));

     
    }


    public PCloudFileBrowserViewModel PCloudFileBrowserViewModel { get; set; }

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

    public override string Header => "Cloud service";

    #region OnActivation

    private bool wasLoaded;
    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (wasLoaded)
      {
        PCloudFileBrowserViewModel.OnBaseDirectoryPathChanged("0");

        var folders = await cloudService.GetFoldersAsync(0);

        if (folders != null)
        {
          wasLoaded = true;
        }
      }

      if (!cloudService.IsUserLoggedIn() && !wasLoaded)
      {
        var vm = new LoginPromptViewModel();

        windowManager.ShowPrompt<LoginPrompt>(vm);

        cloudService.SaveLoginInfo(vm.Name, vm.Password);

        PCloudFileBrowserViewModel.OnBaseDirectoryPathChanged("0");

        var folders = await cloudService.GetFoldersAsync(0);

        if (folders != null)
        {
          wasLoaded = true;
        }
      }
    }

    #endregion
  }
}
