using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using UPnP;
using UPnP.Device;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.ItemsCollections;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.UPnP.Views;
using UPnPService = global::UPnP.UPnPService;
namespace VPlayer.UPnP.ViewModels
{
  public class UPnPManagerViewModel : RegionViewModel<UPnPManagerView>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;
    private UPnPService uPnPService;

    public UPnPManagerViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #region Properties

    public override string Header => "UPnP";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    #region IsDiscovering

    private bool isDiscovering;

    public bool IsDiscovering
    {
      get { return isDiscovering; }
      set
      {
        if (value != isDiscovering)
        {
          isDiscovering = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MediaServers

    private ItemsViewModel<MediaServerViewModel> mediaServers = new ItemsViewModel<MediaServerViewModel>();

    public ItemsViewModel<MediaServerViewModel> MediaServers
    {
      get { return mediaServers; }
      set
      {
        if (value != mediaServers)
        {
          mediaServers = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Renderers

    private ItemsViewModel<MediaRendererViewModel> renderers = new ItemsViewModel<MediaRendererViewModel>();

    public ItemsViewModel<MediaRendererViewModel> Renderers
    {
      get { return renderers; }
      set
      {
        if (value != renderers)
        {
          renderers = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region Discover

    private ActionCommand discover;

    public ICommand Discover
    {
      get
      {
        if (discover == null)
        {
          discover = new ActionCommand(OnDiscover);
        }

        return discover;
      }
    }

    private void OnDiscover()
    {
      DiscoverDevices();
    }

    #endregion

    #endregion

    #region Methods

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      var mediaServers = storageManager.GetRepository<UPnPMediaServer>().Include(x => x.UPnPDevice).ThenInclude(x => x.Services).ToList();

      foreach (var dbMediaServer in mediaServers)
      {
        var mediaServer = new MediaServer()
        {
          AliasURL = dbMediaServer.AliasURL,
          PresentationURL = dbMediaServer.PresentationURL,
          DeviceDescription = new global::UPnP.Common.DeviceDescription()
          {
            Device = dbMediaServer.UPnPDevice.GetDevice()
          },
          OnlineServer = dbMediaServer.OnlineServer,
          DefaultIconUrl = dbMediaServer.DefaultIconUrl,
          ContentDirectoryControlUrl = dbMediaServer.ContentDirectoryControlUrl
        };

        var vm = viewModelsFactory.Create<MediaServerViewModel>(mediaServer);

        vm.IsStored = true;

        MediaServers.Add(vm);
      }

      MediaServers.SelectedItem = MediaServers.View.FirstOrDefault();
    }

    #endregion

    #region DiscoverDevices

    private void DiscoverDevices()
    {
      uPnPService = new UPnPService();

      IsDiscovering = true;

      uPnPService.OnMediaServerFound += Service_OnMediaServerFound;
      uPnPService.OnMediaRendererFound += UPnPService_OnMediaRendererFound;

      uPnPService.StartUPnPDiscoveryAsync();
    }

    #endregion

    #region UPnPService_OnMediaRendererFound

    private void UPnPService_OnMediaRendererFound(object sender, UPnPService.MediaRendererFoundEventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
       
          Renderers.Add(viewModelsFactory.Create<MediaRendererViewModel>(e.MediaRenderer));
      });
    }

    #endregion

    #region Service_OnMediaServerFound

    private void Service_OnMediaServerFound(object sender, UPnPService.MediaServerFoundEventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        var found = MediaServers.ViewModels.Any(x => x.Model.AliasURL == e.MediaServer.AliasURL);

        if (!found)
        {
          var server = viewModelsFactory.Create<MediaServerViewModel>(e.MediaServer);

          MediaServers.Add(server);
        }
      });
    }

    #endregion

    #endregion
  }
}
