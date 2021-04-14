using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UPnP.Common;
using UPnP.Device;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;

namespace VPlayer.UPnP.ViewModels.UPnP.TreeViewItems
{
  public class UPnPContainerViewModel : UPnPTreeViewItem<Container>
  {
    private readonly MediaServer mediaServer;
    private readonly IViewModelsFactory viewModelsFactory;

    public UPnPContainerViewModel(
      Container model,
      MediaServer mediaServer,
      IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.mediaServer = mediaServer ?? throw new ArgumentNullException(nameof(mediaServer));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      Name = Model.Title;
      HighlitedText = Name;
      CanExpand = Model.ChildCount > 0;
    }

 
    #region OnExpanded

    protected override void OnExpanded(bool isExpandend)
    {
      IsBusy = true;

      Task.Run(async () =>
      {
        var results = await mediaServer.BrowseFolderAsync(Model.Id);

        if (results != null)
          CreateViewModelsFromDIDLite(results);
      });

    }

    #endregion

    #region CreateViewModelsFromDIDLite

    private void CreateViewModelsFromDIDLite(DIDLLite dIDLLite)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SubItems.Clear();

        foreach (var result in dIDLLite.Containers)
        {
          var container = viewModelsFactory.Create<UPnPContainerViewModel>(result, mediaServer);

          SubItems.Add(container);
        }

        foreach (var result in dIDLLite.Items)
        {
          var item = viewModelsFactory.Create<UPnPItemViewModel>(result);

          item.FileType = VCore.Standard.ViewModels.WindowsFile.FileType.Sound;

          SubItems.Add(item);
        }
      });

      IsBusy = false;
    }

    #endregion 
  }
}