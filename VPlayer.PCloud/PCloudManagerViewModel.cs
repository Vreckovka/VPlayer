using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PCloudClient;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.Prompts;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Prompt;
using VPlayer.Core;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.FileBrowser;
using VPlayer.Core.ViewModels.FileBrowser.PCloud;
using VPlayer.PCloud.Views;

namespace VPlayer.PCloud
{
  public class PCloudManagerViewModel : RegionViewModel<PCloudManagementView>
  {
    private readonly IPCloudService cloudService;
    private readonly IWindowManager windowManager;

    public PCloudManagerViewModel(
      IRegionProvider regionProvider,
      IPCloudService cloudService,
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
    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      Task.Run(async () =>
      {
        await Task.Delay(50);
        VSynchronizationContext.PostOnUIThread(async () =>
        {
          if (!cloudService.IsUserLoggedIn())
          {
            var vm = new LoginPromptViewModel();


            var result = windowManager.ShowQuestionPrompt<LoginPrompt, LoginPromptViewModel>(vm);

            if (result == PromptResult.Ok)
            {
              cloudService.SaveLoginInfo(vm.Name, vm.Password);
            }
          }

          if (!wasLoaded && cloudService.IsUserLoggedIn())
          {
            wasLoaded = await PCloudFileBrowserViewModel.SetUpManager();
          }
        });
      });
    }

    #endregion
  }
}
