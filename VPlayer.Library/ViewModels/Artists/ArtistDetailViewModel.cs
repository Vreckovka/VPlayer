using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Managers;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
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
    private readonly AudioInfoDownloader audioInfoDownloader;

    public ArtistDetailViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IAlbumsViewModel albumsViewModel,
      IStorageManager storageManager,
      IStatusManager statusManager,
      ArtistViewModel model,
      IWindowManager windowManager,
      AudioInfoDownloader audioInfoDownloader) : base(regionProvider, storageManager, statusManager, model, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
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




    #region ArtistInfoViewModel

    private ArtistInfoViewModel artistInfoViewModel;

    public ArtistInfoViewModel ArtistInfoViewModel
    {
      get { return artistInfoViewModel; }
      set
      {
        if (value != artistInfoViewModel)
        {
          artistInfoViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


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

      OnUpdate();
    }

    #region LoadEntity

    protected override Task LoadEntity()
    {
      return Task.Run(() =>
      {
        var albumsDb = storageManager.GetTempRepository<Album>()
          .Where(x => x.Artist == ViewModel.Model)
          .Include(x => x.Songs)
          .ThenInclude(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        await VSynchronizationContext.InvokeOnDispatcherAsync(async () =>
        {
          allSong = new List<SongDetailViewModel>();
          Albums = (await albumsViewModel.GetViewModelsAsync()).Where(x => albumsDb.Select(y => y.Id).Contains(x.ModelId)).OrderBy(x => x.Name).ToList();

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

    protected override async void OnUpdate()
    {
      var artistInfo = await audioInfoDownloader.GetArtistInfo(ViewModel.Name);
      var vm = viewModelsFactory.Create<ArtistInfoViewModel>(ViewModel);


      if (artistInfo == null && !string.IsNullOrEmpty(ViewModel.Name))
      {
        var name = Regex.Split(ViewModel.Name, @"(?<!^)(?=[A-Z])").Aggregate((x,y) => x +  y);

        artistInfo = await audioInfoDownloader.GetArtistInfo(name);
      }


      vm.ArtistInfo = artistInfo;

      var albumNames = Albums.Select(x => x.Name).ToList();
      var dates = Albums.Select(x => x.Model.ReleaseDate).ToList();

      vm.releaseGroupViewModels?.Where(x =>
        (albumNames.Count(y => x.Model.Title == y || x.Model.Title.Similarity(y) > 0.85) > 0) ||
        (dates.Contains(x.Model.FirstReleaseDate?.ToString())
         && x.Model.FirstReleaseDate != null
         && x.IsOfficial)
      ).ForEach(x => x.IsInLibrary = true);

      ArtistInfoViewModel = vm;

      base.OnUpdate();
    }

    #endregion


  }

  public class ArtistInfoViewModel : ViewModel<ArtistViewModel>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    public List<ReleaseGroupViewModel> releaseGroupViewModels;
    public ArtistInfoViewModel(ArtistViewModel model, IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #region ArtistInfo

    private IArtist artistInfo;

    public IArtist ArtistInfo
    {
      get { return artistInfo; }
      set
      {
        if (value != artistInfo)
        {
          artistInfo = value;
          releaseGroupViewModels = ArtistInfo?.ReleaseGroups?.Select(x => new ReleaseGroupViewModel(x)).ToList();

          releaseGroupViewModels?.Where(x => IsOffical(x.Model)).ForEach(x => x.IsOfficial = true);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OfficialReleasGroups

    public IEnumerable<ReleaseGroupViewModel> OfficialReleasGroups
    {
      get
      {
        return releaseGroupViewModels?.Where(x => x.IsOfficial);
      }
    }

    #endregion

    #region NotOfficialReleaseGroups

    public IEnumerable<ReleaseGroupViewModel> NotOfficialReleasGroups
    {
      get
      {
        return releaseGroupViewModels?.Where(x => !x.IsOfficial);
      }
    }

    #endregion

    #region IsOffical

    public static bool IsOffical(IReleaseGroup g)
    {
      try
      {
        if (g.FirstReleaseDate == null)
          return false;
        if (g.PrimaryType != null)
        {
          return g.PrimaryType.Equals("album", StringComparison.OrdinalIgnoreCase)
                 && g.SecondaryTypes.Count == 0
                 && g.FirstReleaseDate != null;
        }

        return false;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    #endregion
  }

  public class ReleaseGroupViewModel : ViewModel<IReleaseGroup>
  {
    public ReleaseGroupViewModel(IReleaseGroup model) : base(model)
    {
    }

    #region IsInLibrary

    private bool isInLibrary;

    public bool IsInLibrary
    {
      get { return isInLibrary; }
      set
      {
        if (value != isInLibrary)
        {
          isInLibrary = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsOfficial

    private bool isOfficial;

    public bool IsOfficial
    {
      get { return isOfficial; }
      set
      {
        if (value != isOfficial)
        {
          isOfficial = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}