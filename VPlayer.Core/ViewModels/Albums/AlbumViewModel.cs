using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public override string BottomText => GetBottomText();
    public override string ImageThumbnail => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();
    public string Image => !string.IsNullOrEmpty(Model.AlbumFrontCoverFilePath) ? Model.AlbumFrontCoverFilePath : GetEmptyImage();

    #endregion 

    #region Methods

    #region GetSongsToPlay

    public override Task<IEnumerable<SongInPlayListViewModel>> GetItemsToPlay()
    {
      return Task.Run(() =>
      {
        var songs = storage.GetRepository<Song>()
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .Include(x => x.Album)
          .Where(x => x.Album.Id == Model.Id).ToList();

        var playListSong = songs.Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x));

        return playListSong;
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