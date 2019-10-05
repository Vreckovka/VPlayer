using Prism.Events;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using VCore.Factories;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Core.ViewModels.Artists
{
  public interface IPlayableViewModel<TModel> where TModel : INamedEntity
  {
    #region Properties

    int ModelId { get; }
    string Name { get; }

    #endregion Properties

    #region Methods

    void Update(TModel updateItem);

    #endregion Methods
  }

  public class ArtistViewModel : PlayableViewModel<Artist>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionManager vPlayerRegionManager;

    #endregion Fields

    #region Constructors

    public ArtistViewModel(
      Artist artist,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IViewModelsFactory viewModelsFactory,
      IVPlayerRegionManager vPlayerRegionManager) : base(artist, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.vPlayerRegionManager = vPlayerRegionManager ?? throw new ArgumentNullException(nameof(vPlayerRegionManager));
    }

    #endregion Constructors

    #region Properties

    public override string BottomText => Model.Albums?.Count.ToString();
    public override byte[] ImageThumbnail => Model.ArtistCover != null ? Model.ArtistCover : ConvertImageToByte("C:\\Users\\Roman Pecho\\source\\repos\\VPlayer\\VPlayer\\Resources\\VPlayer_Artist.png");

    #endregion Properties

    #region Methods

    public override IEnumerable<SongInPlayList> GetSongsToPlay()
    {
      var songs = storage.GetRepository<Artist>().Include(x => x.Albums.Select(y => y.Songs)).Where(x => x.Id == Model.Id).ToList();
      return songs.SelectMany(x => x.Albums.SelectMany(y => y.Songs)).Select(x => viewModelsFactory.Create<SongInPlayList>(x));
    }

    public override void Update(Artist updateItem)
    {
      Model.Albums = Model.Albums;
    }

    protected override void OnDetail()
    {
      vPlayerRegionManager.ShowArtistDetail(this);
    }

    #endregion Methods
  }
}