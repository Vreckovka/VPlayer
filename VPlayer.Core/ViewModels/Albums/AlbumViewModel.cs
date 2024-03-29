﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Comparers;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.SoundItems;
using VPLayer.Domain;

namespace VPlayer.Core.ViewModels.Albums
{
  public class AlbumViewModel : PlayableViewModelWithThumbnail<SongInPlayListViewModel, Album>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerCloudService iVPlayerCloudService;
    private readonly IVPlayerRegionProvider ivPlayerRegionProvider;

    #endregion Fields

    #region Constructors

    public AlbumViewModel(
      Album model,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IAlbumsViewModel albumsViewModel,
      IViewModelsFactory viewModelsFactory,
      IVPlayerCloudService iVPlayerCloudService,
      IVPlayerRegionProvider ivPlayerRegionProvider) : base(model, eventAggregator, storage)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iVPlayerCloudService = iVPlayerCloudService ?? throw new ArgumentNullException(nameof(iVPlayerCloudService));
      this.ivPlayerRegionProvider = ivPlayerRegionProvider ?? throw new ArgumentNullException(nameof(ivPlayerRegionProvider));

    }

    #endregion Constructors

    #region Properties

    public override string BottomText => GetBottomText();
    public override string ImageThumbnail => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();
    public string Image => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();

    #region HighQualityCover

    private byte[] highQualityCover;

    public byte[] HighQualityCover
    {
      get { return highQualityCover; }
      set
      {
        if (value != highQualityCover)
        {
          highQualityCover = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion 

    #region Methods

    #region GetSongsToPlay

    public override Task<IEnumerable<SongInPlayListViewModel>> GetItemsToPlay()
    {
      return Task.Run(async () =>
      {
        try
        {

          var songs = storage.GetTempRepository<Song>()
            .Include(x => x.ItemModel)
            .ThenInclude(x => x.FileInfoEntity)
            .Include(x => x.Album)
            .ThenInclude(x => x.Artist)
            .Where(x => x.Album.Id == Model.Id).ToList();

          var myComparer = new NumberStringComparer();

          var songsAll = songs.Where(x => x.Source != null).OrderBy(x => x.Source.Split("\\").Last(), myComparer).ToList();

          return songsAll.Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x));
        }
        finally
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            albumsViewModel.LoadingStatus.IsLoading = false;
          });
        }
      });
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<SongInPlayListViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<SongInPlayListViewModel>(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Publish(e);
    }

    #endregion

    #region PublishAddToPlaylistEvent

    public override void PublishAddToPlaylistEvent(IEnumerable<SongInPlayListViewModel> viewModels)
    {
      var e = new PlayItemsEventData<SongInPlayListViewModel>(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Publish(e);
    }

    protected override PinnedType GetPinnedType(Album model)
    {
      return PinnedType.Album;
    }

    #endregion

    #region Update

    public override void Update(Album updateItem)
    {
      Model.Update(updateItem);

      RaisePropertyChanges();
    }

    #endregion

    #region RaisePropertyChanges

    public override void RaisePropertyChanges()
    {
      base.RaisePropertyChanges();

      RaisePropertyChanged(nameof(InfoDownloadStatus));
    }

    #endregion

    #region OnDetail

    protected override void OnDetail()
    {
      ivPlayerRegionProvider.ShowAlbumDetail(this);
    }

    #endregion

    #region GetBottomText

    private string GetBottomText()
    {
      string artistPart = "";
      string albumPart = "";
      string delimiter = "";

      if (!string.IsNullOrEmpty(Model.Artist?.Name))
      {
        artistPart = Model.Artist?.Name + "";
      }

      if (Model.Songs != null && Model.Songs.Count > 0)
      {
        albumPart = Model.Songs.Count + " songs";
      }

      if (!string.IsNullOrEmpty(artistPart) && !string.IsNullOrEmpty(albumPart))
      {
        delimiter = " , ";
      }

      return $"{artistPart}{delimiter}{albumPart}";
    }

    #endregion

    #endregion
  }
}