using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.Views.Music.Artists;

namespace VPlayer.Home.ViewModels.Artists
{
  public class ArtistDetailViewModel : DetailViewModel<ArtistViewModel, Artist, ArtistDetailView>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IStorageManager storageManager;

    public ArtistDetailViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IAlbumsViewModel albumsViewModel,
      IStorageManager storageManager,
      IStatusManager statusManager,
      ArtistViewModel model,
      IWindowManager windowManager) : base(regionProvider, storageManager, statusManager,model, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #region Properties

    #region Albums

    private ICollection<AlbumViewModel> albums;

    public ICollection<AlbumViewModel> Albums
    {
      get { return albums; }
      set
      {
        if (value != albums)
        {
          albums = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;


    #region Songs

    private VirtualList<SongDetailViewModel> songs;
    private List<SongDetailViewModel> allSong;
    public VirtualList<SongDetailViewModel> Songs
    {
      get { return songs; }
      set
      {
        if (value != songs)
        {
          songs = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    #region TotalLength

    private TimeSpan? totalLength;

    public TimeSpan? TotalLength
    {
      get { return totalLength; }
      set
      {
        if (value != totalLength)
        {
          totalLength = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      storageManager.OnItemChanged.Where(x => x.Changed == Changed.Updated)
        .ObserveOn(Application.Current.Dispatcher)
        .OfType<ItemChanged<Song>>()
        .Subscribe(OnSongUpdated).DisposeWith(this);
    }

    #region LoadEntity

    protected override  Task LoadEntity()
    {
      return Task.Run(async () =>
      {
        var albumsDb = storageManager.GetRepository<Album>()
          .Where(x => x.Artist == ViewModel.Model)
          .Include(x => x.Songs)
          .ThenInclude(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
          allSong = new List<SongDetailViewModel>();
          Albums = (await albumsViewModel.GetViewModelsAsync()).Where(x => albumsDb.Select(y => y.Id).Contains(x.ModelId)).ToList();

          foreach (var album in Albums)
          {
            album.Model = albumsDb.SingleOrDefault(x => x.Id == album.ModelId);
            album.Model.Artist = ViewModel.Model;

            if (album.Model?.Songs != null)
            {
              foreach (var song in album.Model.Songs)
              {
                var songD = viewModelsFactory.Create<SongDetailViewModel>(song);

                songD.Info = album.Name;

                allSong.Add(songD);
              }
            }
          }


          var generator = new ItemsGenerator<SongDetailViewModel>(allSong, 25);

          Songs = new VirtualList<SongDetailViewModel>(generator);
          TotalLength = TimeSpan.FromSeconds(allSong.Sum(x => x.Model.Duration));
        });
      });
    }

    #endregion


    private void OnSongUpdated(ItemChanged<Song> song)
    {
      var existingSOng = allSong?.SingleOrDefault(x => x.Model.Id == song.Item.Id);

      if (existingSOng != null)
      {
        existingSOng.Model.Update(song.Item);
        existingSOng.RaisePropertyChanges();
      }
    }

    #endregion


  }
}