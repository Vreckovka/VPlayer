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

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayList : ItemInPlayList<Song>
  {
    #region Fields

    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly GoogleDriveLrcProvider googleDriveLrcProvider;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public SongInPlayList(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      AudioInfoDownloader audioInfoDownloader,
      Song model,
      GoogleDriveLrcProvider googleDriveLrcProvider,
      IStorageManager storageManager) : base(model, eventAggregator)
    {
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.googleDriveLrcProvider = googleDriveLrcProvider ?? throw new ArgumentNullException(nameof(googleDriveLrcProvider));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #endregion Constructors

    #region Properties
   
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }
   
    public string ImagePath => AlbumViewModel.Model?.AlbumFrontCoverFilePath;

    public string LRCLyrics => Model.LRCLyrics;

    #region UpdateIsFavorite
    
    protected override async void UpdateIsFavorite(bool value)
    {
      if (value != Model.IsFavorite)
      {
        var oldVAlue = Model.IsFavorite;
        Model.IsFavorite = value;

        var updated = await storageManager.UpdateEntity(Model);

        if (updated)
        {
          RaisePropertyChanged(nameof(IsFavorite));
        }
        else
        {
          Model.IsFavorite = oldVAlue;
        }
      }
    }

    #endregion

    #region OnActualPositionChanged

    protected override void OnActualPositionChanged(float value)
    {
      UpdateSyncedLyrics();
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

    #region Lyrics

    public string Lyrics
    {
      get
      {
        return Model.Chartlyrics_Lyric;
      }
      set
      {
        if (value != Model.Chartlyrics_Lyric)
        {
          Model.Chartlyrics_Lyric = value;
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
      var songs = new System.Collections.Generic.List<SongInPlayList>() { this };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayList>>().Publish(new RemoveFromPlaylistEventArgs<SongInPlayList>()
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

      AlbumViewModel = (await albumsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == Model.Album.Id);
      ArtistViewModel = (await artistsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == AlbumViewModel.Model.Artist.Id);

      if (AlbumViewModel != null)
        AlbumViewModel.IsInPlaylist = true;

      if (ArtistViewModel != null)
        ArtistViewModel.IsInPlaylist = true;
    }

    #endregion

    #region PublishPlayEvent

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SongInPlayList>>().Publish(this);
    }

    #endregion

    #region PublishRemoveFromPlaylist

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<SongInPlayList>() { this };

      var args = new RemoveFromPlaylistEventArgs<SongInPlayList>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayList>>().Publish(args);
    }

    #endregion
    
    #region Update

    public void Update(Song song)
    {
      Model.Update(song);

      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(Lyrics));
      RaisePropertyChanged(nameof(LyricsObject));
    }

    #endregion

    #region TryGetLRCLyrics

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

        var lrc = parser.Parse(LRCLyrics.Split('\n').ToList());

        if (lrc != null)
        {
          switch (provider)
          {
            case LRCProviders.NotIdentified:
              break;
            case LRCProviders.Google:
              LRCFile = new LRCFileViewModel(lrc, provider, googleDriveLrcProvider);
              break;
            case LRCProviders.Local:
              var localProvider = new LocalLrcProvider("C:\\Lyrics");
              LRCFile = new LRCFileViewModel(lrc, provider, localProvider);
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
    }

    #endregion

    #region LoadLRCFromLocal

    private async Task LoadLRCFromLocal()
    {
      if (!string.IsNullOrEmpty(Model.DiskLocation))
      {
        var provider = new LocalLrcProvider("C:\\Lyrics");

        var lrc = await audioInfoDownloader.TryGetLRCLyricsAsync(provider, Model, ArtistViewModel?.Name, AlbumViewModel?.Name);

        if (lrc != null)
          LRCFile = new LRCFileViewModel(lrc, provider.LRCProvider, provider);

      }
    }

    #endregion

    #region LoadLRCFromGoogleDrive

    private async Task LoadLRCFromGoogleDrive()
    {
      var lrc = (GoogleLRCFile)await audioInfoDownloader.TryGetLRCLyricsAsync(googleDriveLrcProvider, Model, ArtistViewModel?.Name, AlbumViewModel?.Name);

      if (lrc != null)
        LRCFile = new LRCFileViewModel(lrc, googleDriveLrcProvider.LRCProvider, googleDriveLrcProvider);

    }

    #endregion

    #region UpdateSyncedLyrics

    private void UpdateSyncedLyrics()
    {
      if (LRCFile != null)
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          LRCFile.SetActualLine(ActualTime);
        });
      }
    }

    #endregion

    #region LoadLRCFromMiniLyrics

    private async Task LoadLRCFromMiniLyrics()
    {
      var client = new MiniLyricsLRCProvider();

      var lrcString = await client.FindLRC(ArtistViewModel.Name, Name);

      var parser = new LRCParser();

      if (lrcString != null)
      {

        var lrc = parser.Parse(lrcString.Replace("\r", "").Split('\n').ToList());

        Model.LRCLyrics = lrcString;

        LRCFile = new LRCFileViewModel(lrc, LRCProviders.MiniLyrics, client);
      }

    }

    #endregion

    #region TryToRefreshUpdateLyrics

    public async Task TryToRefreshUpdateLyrics()
    {
      LRCFile = null;
      Lyrics = null;

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
        await audioInfoDownloader.UpdateSongLyricsAsync(ArtistViewModel.Name, Name, Model);
      }
    }

    #endregion

    #region UpdateAlbumViewModel

    public void UpdateAlbumViewModel(Album album)
    {
      if (AlbumViewModel != null && AlbumViewModel.Model != null)
      {
        AlbumViewModel.Model.Update(album);
        RaisePropertyChanged(nameof(ImagePath));
      }
    }

    #endregion

    #endregion
  }
}