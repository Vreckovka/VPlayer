using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Prism.Events;
using VCore;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;

namespace VPlayer.Core.ViewModels.SoundItems
{
  public class SongInPlayListViewModel : SoundItemInPlaylistViewModel
  {
    #region Fields

    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly PCloudLyricsProvider pCloudLyricsProvider;
    private readonly ILogger logger;
    //private readonly GoogleDriveLrcProvider googleDriveLrcProvider;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public SongInPlayListViewModel(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      AudioInfoDownloader audioInfoDownloader,
      PCloudLyricsProvider pCloudLyricsProvider,
      Song model,
      ILogger logger,
      GoogleDriveLrcProvider googleDriveLrcProvider,
      IStorageManager storageManager) : base(model.ItemModel, eventAggregator, storageManager)
    {
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.pCloudLyricsProvider = pCloudLyricsProvider ?? throw new ArgumentNullException(nameof(pCloudLyricsProvider));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      //this.googleDriveLrcProvider = googleDriveLrcProvider ?? throw new ArgumentNullException(nameof(googleDriveLrcProvider));
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

          if (!string.IsNullOrEmpty(value))
            SaveTextLyrics(value);

          RaisePropertyChanged(nameof(LyricsObject));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public bool IsInEditMode { get; set; }
    public LRCCreatorViewModel LRCCreatorViewModel { get; set; }

    #region LyricsObject

    public object LyricsObject
    {
      get
      {
        if (LRCCreatorViewModel != null && IsInEditMode)
        {
          return LRCCreatorViewModel;
        }

        if (IsAutomaticLyricsDownloadDisabled)
        {
          return false;
        }

        if (LRCFile != null)
        {
          return LRCFile;
        }
        else if (!string.IsNullOrEmpty(Lyrics))
        {
          return Lyrics;
        }

        return null;
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

    #region IsAutomaticLyricsDownloadDisabled

    public bool IsAutomaticLyricsDownloadDisabled
    {
      get { return !Model.IsAutomaticLyricsFindEnabled; }
      set
      {
        if (!value != Model.IsAutomaticLyricsFindEnabled)
        {
          Model.IsAutomaticLyricsFindEnabled = !value;

          if (Model.IsAutomaticLyricsFindEnabled)
          {
            OnIsAutomaticLyricsDownloadDisabled(true);
          }
          else
          {
            Lyrics = null;
            LRCFile = null;
          }

          UpdateDbModel();

          RaisePropertyChanged();
          RaiseNotifyPropertyChanged(nameof(LyricsObject));
        }
      }
    }

    #endregion

    private async void OnIsAutomaticLyricsDownloadDisabled(bool newValue)
    {
      if (newValue)
        await TryToRefreshUpdateLyrics();
    }

    private async void UpdateDbModel()
    {
      var reuslt = await storageManager.UpdateEntityAsync(Model);
    }


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

    private async void OnRefresh()
    {
      await TryToRefreshUpdateLyrics();
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

    protected override void PublishDeleteFile()
    {
      var songs = new List<SongInPlayListViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<SongInPlayListViewModel>()
      {
        DeleteType = DeleteType.File,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Publish(args);
    }

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
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLyricsProvider);
              break;
            case LRCProviders.Google:
              //LRCFile = new LRCFileViewModel(lrc, provider, pCloudLyricsProvider, googleDriveLrcProvider);

              //LRCFile.Model.Title = Name;
              //LRCFile.Model.Artist = artistViewModel?.Name;
              //LRCFile.Model.Album = albumViewModel?.Name;

              //if (LRCFile != null)
              //{
              //  LRCFile.OnApplyPernamently();
              //}

              break;
            case LRCProviders.Local:
              var localProvider = new LocalLrcProvider("C:\\Lyrics");
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLyricsProvider, localProvider);
              break;
            case LRCProviders.MiniLyrics:
              break;
            case LRCProviders.PCloud:
              LRCFile = new LRCFileViewModel(lrc, provider, pCloudLyricsProvider, pCloudLyricsProvider);
              break;
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
          LRCFile = new LRCFileViewModel(lrc, provider.LRCProvider, pCloudLyricsProvider, provider);

      }
    }

    #endregion

    //#region LoadLRCFromGoogleDrive

    //private async Task LoadLRCFromGoogleDrive()
    //{
    //  var lrc = (GoogleLRCFile)await audioInfoDownloader.TryGetLRCLyricsAsync(googleDriveLrcProvider, SongModel, ArtistViewModel?.Name, AlbumViewModel?.Name);

    //  if (lrc != null)
    //    LRCFile = new LRCFileViewModel(lrc, googleDriveLrcProvider.LRCProvider, pCloudLyricsProvider, googleDriveLrcProvider);

    //}

    //#endregion

    #region LoadLRCFromPCloud

    private async Task LoadLRCFromPCloud()
    {
      var lrc = (PCloudLRCFile)await audioInfoDownloader.TryGetLRCLyricsAsync(pCloudLyricsProvider, SongModel, ArtistViewModel?.Name, AlbumViewModel?.Name);

      if (lrc != null)
      {
        var asd = default(IGoogleDriveServiceProvider);
        LRCFile = new LRCFileViewModel(lrc, pCloudLyricsProvider.LRCProvider, pCloudLyricsProvider, pCloudLyricsProvider);
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

        LRCFile = new LRCFileViewModel(lrc, LRCProviders.MiniLyrics, pCloudLyricsProvider, client);
      }

    }

    #endregion

    #region TryToRefreshUpdateLyrics

    public async Task<bool> TryToRefreshUpdateLyrics()
    {
      try
      {
        LRCFile = null;
        Lyrics = null;

        if (IsAutomaticLyricsDownloadDisabled)
        {
          return false;
        }

        if (ArtistViewModel == null || AlbumViewModel == null)
          return false;

        if (LRCFile == null)
        {
          await LoadLRCFromPCloud();

          if (LRCFile == null)
          {
            Lyrics = await pCloudLyricsProvider.GetTextLyrics(SongModel.Name, AlbumViewModel?.Name, ArtistViewModel?.Name);

            if (!string.IsNullOrEmpty(Lyrics))
            {
              RaiseLyricsChange();

              return true;
            }
          }
        }


        //if (LRCFile == null)
        //{
        //  await LoadLRCFromGoogleDrive();

        //  if (LRCFile != null)
        //  {
        //    LRCFile.OnApplyPernamently();
        //  }
        //}

        if (LRCFile == null)
        {
          await LoadLRCFromMiniLyrics();

          if (LRCFile != null)
          {
            LRCFile.OnApplyPernamently();
          }
        }

        if (LRCFile == null)
          await LoadLRCFromLocal();

        if (LRCFile == null)
          LoadLRCFromEnitityLyrics();

        if (LRCFile == null && Lyrics == null && !string.IsNullOrEmpty(ArtistViewModel?.Name))
        {
          await audioInfoDownloader.UpdateSongLyricsAsync(ArtistViewModel.Name, Name, SongModel);
        }

        RaiseLyricsChange();
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }

      return LRCFile != null;
    }

    #endregion

    #region RaiseLyricsChange

    public void RaiseLyricsChange()
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        RaisePropertyChanged(nameof(LRCFile));
        RaisePropertyChanged(nameof(Lyrics));
        RaisePropertyChanged(nameof(LyricsObject));
      });
    }

    #endregion

    #region SaveTextLyrics

    private async void SaveTextLyrics(string lyrics)
    {
      var result = await pCloudLyricsProvider.SaveTextLyrics(SongModel.Name, AlbumViewModel?.Name, ArtistViewModel?.Name, lyrics);

      if (result)
      {
        if (IsAutomaticLyricsDownloadDisabled)
        {
          IsAutomaticLyricsDownloadDisabled = false;
        }

        Lyrics = lyrics;

        RaiseLyricsChange();
      }
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