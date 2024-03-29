﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using UPnP;
using UPnP.Device;
using VCore;

using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.UPnP.ViewModels.UPnP.TreeViewItems;
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
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

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

    #region Initialize

    public override async void Initialize()
    {
      base.Initialize();

      LoadServers();
      LoadRenderers();
    }

    #endregion

    #region OnActivation

    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        MediaServers.OnActualItemChanged.Where(x => x != null).Subscribe(DiscoverServer).DisposeWith(this);
      }
    }

    #endregion

    #region LoadServers

    public Task LoadServers()
    {
      return Task.Run(async () =>
      {
        var mediaServersDb = storageManager.GetTempRepository<UPnPMediaServer>().Include(x => x.UPnPDevice).ThenInclude(x => x.Services).ToList();

        foreach (var dbMediaServer in mediaServersDb)
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

          vm.DbModel = dbMediaServer;
          vm.IsStored = true;

          await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
          {
            MediaServers.Add(vm);
          });
        }

        await VSynchronizationContext.InvokeOnDispatcherAsync(() => { MediaServers.SelectedItem = MediaServers.View.FirstOrDefault(); });

      });
    }

    #endregion

    #region LoadRenderers

    public Task LoadRenderers()
    {
      return Task.Run(async () =>
      {
        var renderersDb = storageManager.GetTempRepository<UPnPMediaRenderer>().Include(x => x.UPnPDevice).ThenInclude(x => x.Services).ToList();

        foreach (var dbMediaRenderer in renderersDb)
        {
          var mediaRenderer = new MediaRenderer()
          {
            PresentationURL = dbMediaRenderer.PresentationURL,
            DeviceDescription = new global::UPnP.Common.DeviceDescription()
            {
              Device = dbMediaRenderer.UPnPDevice.GetDevice()
            }
          };

          var vm = viewModelsFactory.Create<MediaRendererViewModel>(mediaRenderer);

          vm.DbModel = dbMediaRenderer;
          vm.IsStored = true;

          await VSynchronizationContext.InvokeOnDispatcherAsync(() => { Renderers.Add(vm); });

        }

        await VSynchronizationContext.InvokeOnDispatcherAsync(() => { Renderers.SelectedItem = Renderers.View.FirstOrDefault(); });

        await Task.Run(async () =>
        {
          if (Renderers.SelectedItem?.Model != null)
          {
            Renderers.SelectedItem.Model.Init();

            await Renderers.SelectedItem.Model.GetPositionInfoAsync();
          }
        });
      });
    }

    #endregion

    #region DiscoverDevices

    public void DiscoverDevices()
    {
      if (!IsDiscovering)
      {
        uPnPService = new UPnPService();

        IsDiscovering = true;

        uPnPService.OnMediaServerFound += Service_OnMediaServerFound;
        uPnPService.OnMediaRendererFound += UPnPService_OnMediaRendererFound;

        uPnPService.StartUPnPDiscoveryAsync();
      }
      else
      {
        uPnPService?.CancelDiscover();

        IsDiscovering = false;
      }
    }

    #endregion

    #region UPnPService_OnMediaRendererFound

    private void UPnPService_OnMediaRendererFound(object sender, UPnPService.MediaRendererFoundEventArgs e)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        var found = Renderers.ViewModels.Any(x => x.Model.PresentationURL == e.MediaRenderer.PresentationURL);

        if (!found)
        {
          Renderers.Add(viewModelsFactory.Create<MediaRendererViewModel>(e.MediaRenderer));
        }

      });
    }

    #endregion

    #region Service_OnMediaServerFound

    private void Service_OnMediaServerFound(object sender, UPnPService.MediaServerFoundEventArgs e)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        var found = MediaServers.ViewModels.Any(x => x.Model.PresentationURL == e.MediaServer.PresentationURL);

        if (!found)
        {
          var server = viewModelsFactory.Create<MediaServerViewModel>(e.MediaServer);

          MediaServers.Add(server);
        }
      });
    }

    #endregion

    #region DiscoverServer

    public async void DiscoverServer(MediaServerViewModel mediaServerViewModel)
    {
      await mediaServerViewModel.DiscoverMediaServer();
    }

    #endregion

    #endregion
  }
}
