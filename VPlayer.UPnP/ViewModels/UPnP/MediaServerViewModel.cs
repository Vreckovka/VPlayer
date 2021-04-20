using System;
using System.Threading.Tasks;
using UPnP.Common;
using UPnP.Device;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ItemsCollections;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.UPnP.ViewModels.UPnP.TreeViewItems;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public class MediaServerViewModel : UPnPViewModel<MediaServer>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private bool wasDiscovered;
    public MediaServerViewModel(
      MediaServer model,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(model, storageManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #region Properties

    #region IsBusy

    private bool isBusy;

    public bool IsBusy
    {
      get { return isBusy; }
      set
      {
        if (value != isBusy)
        {
          isBusy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Items

    private ItemsViewModel<UPnPTreeViewItem> items = new ItemsViewModel<UPnPTreeViewItem>();

    public ItemsViewModel<UPnPTreeViewItem> Items
    {
      get { return items; }
      set
      {
        if (value != items)
        {
          items = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region DiscoverMediaServer

    public Task DiscoverMediaServer()
    {
      return Task.Run(async () =>
      {

        if (IsBusy)
        {
          return;
        }

        IsBusy = true;


        Model.InitAsync();

        var results = await Model.BrowseFolderAsync("0");

        if (results != null)
          CreateViewModelsFromDIDLite(results);

        IsBusy = false;
      });
    }

    #endregion

    #region CreateViewModelsFromDIDLite

    private void CreateViewModelsFromDIDLite(DIDLLite dIDLLite)
    {
      foreach (var result in dIDLLite.Containers)
      {
        var container = viewModelsFactory.Create<UPnPContainerViewModel>(result, Model);

        Items.Add(container);
      }

      foreach (var result in dIDLLite.Items)
      {
        var container = viewModelsFactory.Create<UPnPItemViewModel>(result);

        Items.Add(container);
      }
    }

    #endregion

    #region StoreData

    public override void StoreData()
    {
      var dbEntity = new UPnPMediaServer()
      {
        AliasURL = Model.AliasURL,
        UPnPDevice = Model.DeviceDescription.Device.GetDeviceDbEntity(),
        PresentationURL = Model.PresentationURL,
        OnlineServer = Model.OnlineServer,
        ContentDirectoryControlUrl = Model.ContentDirectoryControlUrl,
        DefaultIconUrl = Model.DefaultIconUrl
      };

      var result = storageManager.StoreEntity<UPnPMediaServer>(dbEntity, out var uPnPMediaServer);


      if (result)
      {
        IsStored = true;
        save.RaiseCanExecuteChanged();
      }
    }

    #endregion

    #region OnSelected

    public override void OnSelected()
    {
      if (!wasDiscovered)
      {
        wasDiscovered = true;

        DiscoverMediaServer();
      }
    }

    #endregion

    #endregion

  }
}
