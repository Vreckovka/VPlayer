using Prism.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
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
  public class SongInPlayList : ViewModel<Song>
  {
    #region Fields

    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly GoogleDriveLrcProvider googleDriveLrcProvider;
    private readonly IStorageManager storageManager;
    private readonly IEventAggregator eventAggregator;

    #endregion Fields

    #region Constructors

    public SongInPlayList(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      AudioInfoDownloader audioInfoDownloader,
      Song model,
      GoogleDriveLrcProvider googleDriveLrcProvider,
      IStorageManager storageManager) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.googleDriveLrcProvider = googleDriveLrcProvider ?? throw new ArgumentNullException(nameof(googleDriveLrcProvider));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #endregion Constructors

    #region Properties

    #region ActualPosition

    private float actualPosition;

    public float ActualPosition
    {
      get { return actualPosition; }
      set
      {
        if (value != actualPosition)
        {
          actualPosition = value;
          RaisePropertyChanged();

          UpdateSyncedLyrics();
        }
      }
    }
    #endregion

    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }
    public int Duration => Model.Duration;
    public string ImagePath => AlbumViewModel.Model?.AlbumFrontCoverFilePath;
    public bool IsPaused { get; set; }
    public string Name => Model.Name;

    public string LRCLyrics => Model.LRCLyrics;

    #region IsFavorite

    public bool IsFavorite
    {
      get { return Model.IsFavorite; }
      set { UpdateIsFavorite(value); }
    }

    private async void UpdateIsFavorite(bool value)
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

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;

          if (ArtistViewModel != null)
            ArtistViewModel.IsPlaying = isPlaying;

          if (AlbumViewModel != null)
            AlbumViewModel.IsPlaying = isPlaying;

          RaisePropertyChanged();
        }
      }
    }

    #endregion IsPlaying

    #endregion Properties

    #region Commands

    #region DeleteSongFromPlaylist

    private ActionCommand deleteSongFromPlaylist;

    public ICommand DeleteSongFromPlaylist
    {
      get
      {
        if (deleteSongFromPlaylist == null)
        {
          deleteSongFromPlaylist = new ActionCommand(OnDeleteSongFromPlaylist);
        }

        return deleteSongFromPlaylist;
      }
    }

    public void OnDeleteSongFromPlaylist()
    {
      var songs = new System.Collections.Generic.List<SongInPlayList>() { this };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent>().Publish(new RemoveFromPlaylistEventArgs()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        SongsToDelete = songs
      });
    }

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

      eventAggregator.GetEvent<RemoveFromPlaylistEvent>().Publish(new RemoveFromPlaylistEventArgs()
      {
        DeleteType = DeleteType.AlbumFromPlaylist,
        SongsToDelete = songs
      });
    }

    #endregion

    #region Play

    private ActionCommand play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand(OnPlayButton);
        }

        return play;
      }
    }

    public void OnPlay()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Publish(this);
    }

    private void OnPlayButton()
    {
      if (!IsPlaying)
      {
        OnPlay();
      }
      else
      {
        eventAggregator.GetEvent<PauseEvent>().Publish();
      }
    }

    #endregion 

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

    #region OpenContainingFolder

    private ActionCommand openContainingFolder;

    public ICommand OpenContainingFolder
    {
      get
      {
        if (openContainingFolder == null)
        {
          openContainingFolder = new ActionCommand(OnOpenContainingFolder);
        }

        return openContainingFolder;
      }
    }


    private void OnOpenContainingFolder()
    {

      if (!string.IsNullOrEmpty(Model.DiskLocation))
      {
        var folder = Path.GetDirectoryName(Model.DiskLocation);

        if (!string.IsNullOrEmpty(folder))
        {
          Process.Start(folder);
        }
      }
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
        LRCFile = new LRCFileViewModel(lrc, googleDriveLrcProvider.LRCProvider,googleDriveLrcProvider);

    }

    #endregion

    #region UpdateSyncedLyrics

    private void UpdateSyncedLyrics()
    {
      if (LRCFile != null)
      {
        LRCFile.SetActualLine(ActualTime);
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
        await LoadLRCFromLocal();

      if (LRCFile == null)
        LoadLRCFromEnitityLyrics();

      if (!string.IsNullOrEmpty(ArtistViewModel?.Name))
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


  public class LRCLyricLineViewModel : ViewModel<LRCLyricLine>
  {
    public bool IsActual { get; set; }
    public string Text => Model.Text;

    public LRCLyricLineViewModel(LRCLyricLine model) : base(model)
    {
    }
  }
}