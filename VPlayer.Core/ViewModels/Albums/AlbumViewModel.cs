using Prism.Events;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using VCore.Factories;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumViewModel : PlayableViewModel<Album>
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
    public override byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB != null ? Model.AlbumFrontCoverBLOB : GetEmptyImage();

    #endregion Properties

    #region Methods

    #region GetSongsToPlay

    public override IEnumerable<SongInPlayList> GetSongsToPlay()
    {
      var songs = storage.GetRepository<Song>().Include(x => x.Album).Where(x => x.Album.Id == Model.Id).ToList();
      var playListSong = songs.Select(x => viewModelsFactory.Create<SongInPlayList>(x));

      return playListSong;
    }

    #endregion

    #region Update

    public override void Update(Album updateItem)
    {
      Model.AlbumFrontCoverBLOB = updateItem.AlbumFrontCoverBLOB;
      Model.Name = updateItem.Name;

      RaisePropertyChanges();
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