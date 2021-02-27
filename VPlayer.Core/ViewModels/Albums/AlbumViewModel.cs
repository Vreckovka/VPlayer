using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Core.ViewModels.Albums
{
  public class AlbumViewModel : PlayableViewModelWithThumbnail<SongInPlayListViewModel, Album>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionProvider ivPlayerRegionProvider;

    #endregion Fields

    #region Constructors

    public AlbumViewModel(
      Album model,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IViewModelsFactory viewModelsFactory,
      IVPlayerRegionProvider ivPlayerRegionProvider) : base(model, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.ivPlayerRegionProvider = ivPlayerRegionProvider ?? throw new ArgumentNullException(nameof(ivPlayerRegionProvider));

    }

    #endregion Constructors

    #region Properties

    public override string BottomText => $"{Model.Artist?.Name}\nNumber of song: {Model.Songs?.Count.ToString()}";
    public override string ImageThumbnail => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();
    public string Image => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();

    #endregion Properties

    #region Methods

    #region GetSongsToPlay

    public override IEnumerable<SongInPlayListViewModel> GetItemsToPlay()
    {
      var songs = storage.GetRepository<Song>().Include(x => x.Album).Where(x => x.Album.Id == Model.Id).ToList();
      var playListSong = songs.Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x));

      return playListSong;
    }

    #endregion


    public override void PublishPlayEvent(IEnumerable<SongInPlayListViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlaySongsEventData(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SongInPlayListViewModel> viewModels)
    {
      var e = new PlaySongsEventData(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

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

    #endregion Methods
  }
}