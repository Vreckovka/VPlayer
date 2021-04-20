using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UPnP.Common;
using UPnP.Device;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP.TreeViewItems
{
  public class UPnPContainerViewModel : UPnPTreeViewItem<Container>
  {
    private readonly MediaServer mediaServer;
    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;

    public UPnPContainerViewModel(
      Container model,
      MediaServer mediaServer,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.mediaServer = mediaServer ?? throw new ArgumentNullException(nameof(mediaServer));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      Name = Model.Title;
      HighlitedText = Name;
      CanExpand = Model.ChildCount > 0;
    }

    #region Commands

    #endregion

    #region OnExpanded

    protected override void OnExpanded(bool isExpandend)
    {
      IsBusy = true;

      LoadFolder();

    }

    #endregion

    public Task LoadFolder()
    {
      return Task.Run(async () =>
      {
        var results = await mediaServer.BrowseFolderAsync(Model.Id);

        if (results != null)
          CreateViewModelsFromDIDLite(results);
      });
    }


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