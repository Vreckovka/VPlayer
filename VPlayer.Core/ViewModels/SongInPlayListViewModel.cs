using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayListViewModel : SoundItemInPlaylistViewModel
  {
    #region Fields

    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly PCloudLrcProvider pCloudLrcProvider;
    private readonly GoogleDriveLrcProvider googleDriveLrcProvider;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public SongInPlayListViewModel(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      AudioInfoDownloader audioInfoDownloader,
      PCloudLrcProvider pCloudLrcProvider,
      Song model,
      GoogleDriveLrcProvider googleDriveLrcProvider,
      IStorageManager storageManager) : base(model.ItemModel, eventAggregator, storageManager)
    {
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.pCloudLrcProvider = pCloudLrcProvider ?? throw new ArgumentNullException(nameof(pCloudLrcProvider));
      this.googleDriveLrcProvider = googleDriveLrcProvider ?? throw new ArgumentNullException(nameof(googleDriveLrcProvider));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      SongModel = model ?? throw new ArgumentNullException(nameof(model));
    }

    #endregion Constructors

    #region Properties

    #region SongModel

    private Song songModel;

    public Song SongModel
    {
      get { return songModel; }
      set
      {
        if (value != songModel)
        {
          songModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AlbumViewModel

    private AlbumViewModel albumViewModel;

    public AlbumViewModel AlbumViewModel
    {
      get { return albumViewModel; }
      set
      {
        if (value != albumViewModel)
        {
          albumViewModel = value;

          RaisePropertyChanged(nameof(ImagePath));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ArtistViewModel

    private ArtistViewModel artistViewModel;

    public ArtistViewModel ArtistViewModel
    {
      get { return artistViewModel; }
      set
      {
        if (value != artistViewModel)
        {
          artistViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string ImagePath => AlbumViewModel?.Model?.AlbumFrontCoverFilePath;

    public string LRCLyrics => SongModel.LRCLyrics;


    #region OnActualPositionChanged

    protected override void OnActualPositionChanged(float value)
    {
      UpdateSyncedLyrics();
    }

    #endregion

    #region Lyrics

    public string Lyrics
    {
      get
      {
        return SongModel.Chartlyrics_Lyric;
      }
      set
      {
        if (value != SongModel.Chartlyrics_Lyric)
        {
          SongModel.Chartlyrics_Lyric = value;
          RaisePropertyChanged(nameof(LyricsObject));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LyricsObject

    public object LyricsObject
    {
      get
      {
        if (LRCLyrics == null)
        {
          return Lyrics;
        }
        else
        {
          return LRCFile;
        }
      }
    }

    #endregion

    #region LRCFile

    private LRCFileViewModel lRCFile;

    public LRCFileViewModel LRCFile
    {
      get { return lRCFile; }
      set
      {
        if (value != lRCFile)
        {
          lRCFile = value;
          RaisePropertyChanged(nameof(LyricsObject));
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    #endregion

    #region Commands

    #region Refresh

    private ActionCommand refresh;

    public ICommand Refresh
    {
      get
      {
        if (refresh == null)
        {
          refresh = new ActionCommand(OnRefresh);
        }

        return refresh;
      }
    }

    #region OnRefresh

    private void OnRefresh()
    {
      TryToRefreshUpdateLyrics();
    }

    #endregion

    #endregion

    #region DeleteSongFromPlaylistWithAlbum

    private ActionCommand deleteSongFromPlaylistWithAlbum;

    public ICommand DeleteSongFromPlaylistWithAlbum
    {
      get
      {
        if (deleteSongFromPlaylistWithAlbum == null)
        {
          deleteSongFromPlaylistWithAlbum = new ActionCommand(OnDeleteSongFromPlaylistWithAlbum);
        }

        return deleteSongFromPlaylistWithAlbum;
      }
    }

    public void OnDeleteSongFromPlaylistWithAlbum()
    {
      var songs = new System.Collections.Generic.List<SongInPlayListViewModel>() { this };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Publish(new RemoveFromPlaylistEventArgs<SongInPlayListViewModel>()
      {
        DeleteType = DeleteType.AlbumFromPlaylist,
        ItemsToRemove = songs
      });
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override async void Initialize()
    {
      base.Initialize();

      if (SongModel.Album != null)
      {
        AlbumViewModel = (await albumsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == SongModel.Album.Id);

        if (SongModel.Album.Artist != null)
        {
          ArtistViewModel = (await artistsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == SongModel.Album.Artist.Id);
        }
        else if (AlbumViewModel?.Model.Artist != null)
        {
          ArtistViewModel = (await artistsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == AlbumViewModel.Model.Artist.Id);
        }
      }

      if (AlbumViewModel != null)
        AlbumViewModel.IsInPlaylist = true;

      if (ArtistViewModel != null)
        ArtistViewModel.IsInPlaylist = true;

      LoadLRCFromEnitityLyrics();
    }

    #endregion

    #region PublishPlayEvent

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SongInPlayListViewModel>>().Publish(this);
    }

    #endregion

    #region PublishRemoveFromPlaylist

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<SongInPlayListViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<SongInPlayListViewModel>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Publish(args);
    }

    #endregion

    #region Update

    public void Update(Song song)
    {
      SongModel.Update(song);

      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(Lyrics));
      RaisePropertyChanged(nameof(LyricsObject));
    }

    #endregion

    #region LoadLRCFromEnitityLyrics

    public void LoadLRCFromEnitityLyrics()
    {
      if (LRCLyrics != null)
      {
        var parser = new LRCParser();

        var provider = LRCProviders.NotIdentified;

        if (int.TryParse(LRCLyrics.Split(';').First(), out var providerNum))
        {
          provider = (LRCProviders)providerNum;
        }

        var lrc = parser.Parse(LRCLyrics.Replace("\r", null).Split('\n').ToList());

        if (lrc != null)
        {
          switch (provider)
          {
            case LRCProviders.NotIdentified:
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLrcProvider);
              break;
            case LRCProviders.Google:
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLrcProvider, googleDriveLrcProvider);
              break;
            case LRCProviders.Local:
              var localProvider = new LocalLrcProvider("C:\\Lyrics");
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLrcProvider, localProvider);
              break;
            case LRCProviders.MiniLyrics:
              break;
            case LRCProviders.PCloud:
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLrcProvider, pCloudLrcProvider);
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
      else
      {
        RaisePropertyChanged(nameof(LyricsObject));
      }
    }

    #endregion

    #region LoadLRCFromLocal

    private async Task LoadLRCFromLocal()
    {
      if (!string.IsNullOrEmpty(Model.Source))
      {
        var provider = new LocalLrcProvider("C:\\Lyrics");

        var lrc = await audioInfoDownloader.TryGetLRCLyricsAsync(provider, SongModel, ArtistViewModel?.Name, AlbumViewModel?.Name);

        if (lrc != null)
          LRCFile = new LRCFileViewModel(lrc, provider.LRCProvider, pCloudLrcProvider, provider);

      }
    }

    #endregion

    #region LoadLRCFromGoogleDrive

    private async Task LoadLRCFromGoogleDrive()
    {
      var lrc = (GoogleLRCFile)await audioInfoDownloader.TryGetLRCLyricsAsync(googleDriveLrcProvider, SongModel, ArtistViewModel?.Name, AlbumViewModel?.Name);

      if (lrc != null)
        LRCFile = new LRCFileViewModel(lrc, googleDriveLrcProvider.LRCProvider, pCloudLrcProvider, googleDriveLrcProvider);

    }

    #endregion

    #region LoadLRCFromPCloud

    private async Task LoadLRCFromPCloud()
    {
      var lrc = (PCloudLRCFile)await audioInfoDownloader.TryGetLRCLyricsAsync(pCloudLrcProvider, SongModel, ArtistViewModel?.Name, AlbumViewModel?.Name);

      if (lrc != null)
      {
        LRCFile = new LRCFileViewModel(lrc, pCloudLrcProvider.LRCProvider, pCloudLrcProvider, googleDriveLrcProvider);
      }
        

    }

    #endregion

    #region UpdateSyncedLyrics

    private void UpdateSyncedLyrics()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (LRCFile != null)
        {
          LRCFile.SetActualLine(ActualTime);
        }
      });
    }

    #endregion

    #region LoadLRCFromMiniLyrics

    private async Task LoadLRCFromMiniLyrics()
    {
      var client = new MiniLyricsLRCProvider();

      var lrcString = (await client.FindLRC(ArtistViewModel.Name, Name))?.Replace("\r", "");

      var parser = new LRCParser();

      if (lrcString != null)
      {
        var lrc = parser.Parse(lrcString.Split('\n').ToList());

        lrc.Artist = ArtistViewModel?.Name;
        lrc.Album = AlbumViewModel?.Name;
        lrc.Title = Name;

        SongModel.LRCLyrics = lrcString;

        LRCFile = new LRCFileViewModel(lrc, LRCProviders.MiniLyrics, pCloudLrcProvider, client);
      }

    }

    #endregion

    #region TryToRefreshUpdateLyrics

    public async Task<bool> TryToRefreshUpdateLyrics()
    {
      LRCFile = null;
      Lyrics = null;

      if (ArtistViewModel == null || AlbumViewModel == null)
        return false;

      if (LRCFile == null)
        await LoadLRCFromPCloud();

      if (LRCFile == null)
        await LoadLRCFromGoogleDrive();

      if (LRCFile == null)
        await LoadLRCFromMiniLyrics();

      if (LRCFile == null)
        await LoadLRCFromLocal();

      if (LRCFile == null)
        LoadLRCFromEnitityLyrics();

      if (LRCFile == null && Lyrics == null && !string.IsNullOrEmpty(ArtistViewModel?.Name))
      {
        await audioInfoDownloader.UpdateSongLyricsAsync(ArtistViewModel.Name, Name, SongModel);
      }



      Application.Current?.Dispatcher?.Invoke(() =>
      {
        RaisePropertyChanged(nameof(LRCFile));
        RaisePropertyChanged(nameof(Lyrics));
        RaisePropertyChanged(nameof(LyricsObject));
      });

      return LRCFile != null;
    }

    #endregion

    #region UpdateAlbumViewModel

    public void UpdateAlbumViewModel(Album album)
    {
      if (AlbumViewModel?.Model != null)
      {
        AlbumViewModel.Model.Update(album);
        RaisePropertyChanged(nameof(ImagePath));
      }
    }

    #endregion

    #region OnIsPlayingChanged

    protected override void OnIsPlayingChanged(bool value)
    {
      if (ArtistViewModel != null)
        ArtistViewModel.IsPlaying = value;

      if (AlbumViewModel != null)
        AlbumViewModel.IsPlaying = value;
    }

    #endregion

    #endregion
  }
}