using Prism.Events;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using VCore.Factories;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Core.ViewModels.Artists
{
  public interface INamedEntityViewModel<TModel> where TModel : INamedEntity
  {
    #region Properties

    int ModelId { get; }
    string Name { get; }

    #endregion Properties

    #region Methods

    void Update(TModel updateItem);

    #endregion Methods
  }

  public class ArtistViewModel : PlayableViewModelWithThumbnail<Artist>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionProvider ivPlayerRegionProvider;
    string albumCoverPath = null;

    #endregion Fields

    #region Constructors

    public ArtistViewModel(
      Artist artist,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IViewModelsFactory viewModelsFactory,
      IVPlayerRegionProvider ivPlayerRegionProvider) : base(artist, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.ivPlayerRegionProvider = ivPlayerRegionProvider ?? throw new ArgumentNullException(nameof(ivPlayerRegionProvider));
    }

    #endregion Constructors

    #region Properties

    public override string BottomText => Model.Albums?.Count.ToString();
    public override string ImageThumbnail => GetArtistCover();

    #endregion 

    #region Methods

    #region GetSongsToPlay

    public override IEnumerable<SongInPlayList> GetSongsToPlay()
    {
      var songs = storage.GetRepository<Artist>().Include(x => x.Albums.Select(y => y.Songs)).Where(x => x.Id == Model.Id).ToList();
      return songs.SelectMany(x => x.Albums.SelectMany(y => y.Songs)).Select(x => viewModelsFactory.Create<SongInPlayList>(x));
    }

    #endregion

    #region Update

    public override void Update(Artist updateItem)
    {
      if (updateItem.Modified > Model.Modified || Model.Modified == null)
      {
        Model.Albums = Model.Albums;

        if (Model.AlbumIdCover != updateItem.AlbumIdCover)
        {
          albumCoverPath = null;
          Model.AlbumIdCover = updateItem.AlbumIdCover;
        }

        Model.Modified = updateItem.Modified;

        RaisePropertyChanges();
      }
    }

    #endregion

    #region OnDetail

    protected override void OnDetail()
    {
      ivPlayerRegionProvider.ShowArtistDetail(this);
    }

    #endregion

    #region GetArtistCover

    private string GetArtistCover()
    {
      if (!string.IsNullOrEmpty(albumCoverPath))
      {
        return albumCoverPath;
      }
      else if (Model.AlbumIdCover != null && albumCoverPath == null)
      {
        var album = storage.GetRepository<Album>().SingleOrDefault(x => x.Id == Model.AlbumIdCover);

        if (album != null)
        {
          albumCoverPath = album.AlbumFrontCoverFilePath;

          return albumCoverPath;
        }
      }

      return Model.ArtistCover != null ? Model.ArtistCover : GetEmptyImage();
    }

    #endregion

    #endregion 
  }
}