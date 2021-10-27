using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using VCore.Standard;
using VCore.Standard.Comparers;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.SoundItems;
using VPLayer.Domain;

namespace VPlayer.Core.ViewModels.Artists
{
  public interface INamedEntityViewModel<TModel> : IViewModel<TModel> where TModel : INamedEntity
  {
    #region Properties

    int ModelId { get; }
    string Name { get; }

    #endregion Properties

    #region Methods

    void Update(TModel updateItem);

    #endregion Methods
  }

  public class ArtistViewModel : PlayableViewModelWithThumbnail<SongInPlayListViewModel, Artist>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IVPlayerCloudService iVPlayerCloudService;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionProvider ivPlayerRegionProvider;
    string albumCoverPath = null;

    #endregion Fields

    #region Constructors

    public ArtistViewModel(
      Artist artist,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IVPlayerCloudService iVPlayerCloudService,
      IArtistsViewModel artistsViewModel,
      IViewModelsFactory viewModelsFactory,
      IVPlayerRegionProvider ivPlayerRegionProvider) : base(artist, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.iVPlayerCloudService = iVPlayerCloudService ?? throw new ArgumentNullException(nameof(iVPlayerCloudService));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.ivPlayerRegionProvider = ivPlayerRegionProvider ?? throw new ArgumentNullException(nameof(ivPlayerRegionProvider));
    }

    #endregion Constructors

    #region Properties

    public override string BottomPathData => "M227.5 131.2c8.609-1.953 14.03-10.52 12.08-19.14c-1.953-8.609-10.55-13.95-19.14-12.08c-59.7 13.5-107 60.81-120.5 120.5C97.1 229.1 103.4 237.7 112 239.6C113.2 239.9 114.4 240 115.6 240c7.312 0 13.91-5.047 15.59-12.47C141.1 179.8 179.8 141.1 227.5 131.2zM256 159.1c-53.08 0-96 42.92-96 96c0 53.08 42.92 95.1 96 95.1s96-42.92 96-95.1C352 202.9 309.1 159.1 256 159.1zM256 280C242.8 280 232 269.3 232 256S242.8 232 256 232S280 242.8 280 256S269.3 280 256 280zM256 0C114.6 0 0 114.6 0 256s114.6 256 256 256s256-114.6 256-256S397.4 0 256 0zM256 464c-114.7 0-208-93.31-208-208S141.3 48 256 48s208 93.31 208 208S370.7 464 256 464z";
    public override string BottomText => $"{Model.Albums?.Count} albums";
    public override string ImageThumbnail => GetArtistCover();

    #endregion

    #region Methods

    #region GetSongsToPlay

    private SerialDisposable serialDisposable = new SerialDisposable();

    public override Task<IEnumerable<SongInPlayListViewModel>> GetItemsToPlay()
    {
      return Task.Run(async () =>
      {
        bool wasBusySet = false;

        try
        {
          var songs = storage.GetRepository<Artist>()
          .Include(x => x.Albums)
          .ThenInclude(x => x.Songs)
          .ThenInclude(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .Where(x => x.Id == Model.Id).ToList();

          var myComparer = new NumberStringComparer();

          var songsAll = songs
            .SelectMany(x => x.Albums.OrderBy(y => y.ReleaseDate)
            .SelectMany(y => y.Songs.OrderBy(z => z.Source.Split("\\").Last(), myComparer))
            ).ToList();


          var cloudSOngs = songsAll.Where(x => x.Source.Contains("http")).ToList();

          var process = iVPlayerCloudService.GetItemSources(cloudSOngs.Select(x => x.ItemModel.FileInfo));

          if (process.InternalProcessesCount != 0)
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              wasBusySet = true;
              artistsViewModel.LoadingStatus.IsLoading = true;
            });
          }

          Application.Current.Dispatcher.Invoke(() =>
          {
            artistsViewModel.LoadingStatus.NumberOfProcesses = process.InternalProcessesCount;
          });

          serialDisposable.Disposable = process.OnInternalProcessedCountChanged.Subscribe(x =>
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              artistsViewModel.LoadingStatus.ProcessedCount = x;
            });
          });

          await process.Process;

          return songsAll.Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x));
        }
        finally
        {
          if(wasBusySet)
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              artistsViewModel.LoadingStatus.IsLoading = false;
            });
          }
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