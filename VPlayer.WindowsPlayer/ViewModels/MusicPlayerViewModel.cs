using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FFMpegCore;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using PCloudClient.Domain;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Providers;
using VCore.WPF;
using VCore.WPF.Converters;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.Events;
using VCore.WPF.ViewModels.Prompt;
using VFfmpeg;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.GIfs;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud.Images;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Players;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain;
using VPlayer.Home.ViewModels;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.UPnP.ViewModels;
using VPlayer.UPnP.ViewModels.Player;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.ViewModels.Windows;
using VPlayer.WindowsPlayer.Views.Prompts;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using VVLC.Players;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using LibVLCSharp.Shared;
using VVLC;
using VPlayer.WindowsPlayer.Views;


namespace VPlayer.WindowsPlayer.ViewModels
{
  //Mp3Tag
  public class MusicPlayerViewModel : FilePlayableRegionViewModel<WindowsPlayerView, SoundItemInPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem, SoundItem, SoundSliderPopupDetailViewModel>, IMusicPlayerViewModel
  {
    #region Fields

    private readonly IVPlayerRegionProvider vPlayerRegionProvider;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly IVPlayerCloudService cloudService;
    private readonly IPCloudAlbumCoverProvider iPCloudAlbumCoverProvider;
    private readonly IWindowManager windowManager;
    private readonly IStatusManager statusManager;
    private readonly VLCPlayer vLcPlayer;
    private Dictionary<SongInPlayListViewModel, bool> playBookInCycle = new Dictionary<SongInPlayListViewModel, bool>();


    #endregion Fields

    #region Constructors

    public MusicPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager,
      AudioInfoDownloader audioInfoDownloader,
      UPnPManagerViewModel uPnPManagerViewModel,
      IVPlayerCloudService cloudService,
      IPCloudAlbumCoverProvider iPCloudAlbumCoverProvider,
      ILogger logger,
      IWindowManager windowManager,
      IStatusManager statusManager,
      IVFfmpegProvider iVFfmpegProvider,
      ISettingsProvider settingsProvider,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger,
      storageManager, eventAggregator, windowManager, statusManager,
      viewModelsFactory, iVFfmpegProvider, settingsProvider, cloudService, vLCPlayer)
    {
      this.vPlayerRegionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.iPCloudAlbumCoverProvider = iPCloudAlbumCoverProvider ?? throw new ArgumentNullException(nameof(iPCloudAlbumCoverProvider));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      UPnPManagerViewModel = uPnPManagerViewModel ?? throw new ArgumentNullException(nameof(uPnPManagerViewModel));

      SelectedMediaRendererViewModel = uPnPManagerViewModel.Renderers.View.FirstOrDefault();

      vLcPlayer = vLCPlayer ?? throw new ArgumentNullException(nameof(vLCPlayer));
    }



    #endregion Constructors

    #region Properties



    #region VideoVLCPlayer

    private MediaPlayer myVar;

    public MediaPlayer VideoVLCPlayer
    {
      get { return myVar; }
      set
      {
        if (value != myVar)
        {
          myVar = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion




    #region RandomGifUrl

    private string randomGifUrl;

    public string RandomGifUrl
    {
      get { return randomGifUrl; }
      set
      {
        if (value != randomGifUrl)
        {
          randomGifUrl = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region GifTag

    private string gifTag = "random";

    public string GifTag
    {
      get { return gifTag; }
      set
      {
        if (value != gifTag)
        {
          gifTag = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region UseGif

    private bool useGif;
    public bool UseGif
    {
      get { return useGif; }
      set
      {
        if (value != useGif)
        {
          useGif = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    public IEnumerable<long> PCloudIds
    {
      get
      {
        var list = new List<long>();

        foreach (var item in PlayList)
        {
          if (long.TryParse(item.Model.FileInfoEntity.Indentificator, out var id))
          {
            list.Add(id);
          }
        }

        return list;
      }
    }

    public override bool ContainsNestedRegions => true;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override string Header => "Music player";
    public int Cycle { get; set; }

    public UPnPManagerViewModel UPnPManagerViewModel { get; }

    #region MediaRendererViewModel

    private MediaRendererViewModel selectedMediaRendererViewModel;

    public MediaRendererViewModel SelectedMediaRendererViewModel
    {
      get { return selectedMediaRendererViewModel; }
      set
      {
        if (value != selectedMediaRendererViewModel)
        {
          selectedMediaRendererViewModel = value;
          playPauseUPnP?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsUPnPPlaying

    private bool isUPnPPlaying;

    public bool IsUPnPPlaying
    {
      get { return isUPnPPlaying; }
      set
      {
        if (value != isUPnPPlaying)
        {
          isUPnPPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ArtistThatCanBeAddedCount

    private int artistThatCanBeAddedCount;

    public int ArtistThatCanBeAddedCount
    {
      get { return artistThatCanBeAddedCount; }
      set
      {
        if (value != artistThatCanBeAddedCount)
        {
          artistThatCanBeAddedCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AlbumsThatCanBeAddedCount

    private int albumsThatCanBeAddedCount;

    public int AlbumsThatCanBeAddedCount
    {
      get { return albumsThatCanBeAddedCount; }
      set
      {
        if (value != albumsThatCanBeAddedCount)
        {
          albumsThatCanBeAddedCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region WasAllItemsProcessed

    private bool wasAllItemsProcessed;

    public override bool WasAllItemsProcessed
    {
      get { return wasAllItemsProcessed; }
      set
      {
        if (value != wasAllItemsProcessed)
        {
          wasAllItemsProcessed = value;

          addAlbums?.RaiseCanExecuteChanged();
          addArtists?.RaiseCanExecuteChanged();

          if (wasAllItemsProcessed)
          {
            RaisePropertyChanged(nameof(DistinctArtistsCount));
            RaisePropertyChanged(nameof(DistinctAlbumsCount));
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DistinctArtistsCount

    public int DistinctArtistsCount
    {
      get
      {
        return PlayList
    .OfType<SongInPlayListViewModel>()
    .Where(x => x.ArtistViewModel != null)
    .Where(x => !string.IsNullOrEmpty(x.ArtistViewModel.Name))
    .GroupBy(x => x.ArtistViewModel.Model).Count();
      }

    }

    #endregion

    #region DistinctAlbumsCount

    public int DistinctAlbumsCount
    {
      get
      {
        var albums = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.AlbumViewModel != null)
          .Where(x => !string.IsNullOrEmpty(x.AlbumViewModel.Name))
          .GroupBy(x => x.AlbumViewModel.Model);

        return albums.Count();
      }
    }

    #endregion

    #endregion

    #region Commands

    #region AlbumDetail

    private ActionCommand albumDetail;

    public ICommand AlbumDetail
    {
      get
      {
        if (albumDetail == null)
        {
          albumDetail = new ActionCommand(OnAlbumDetail, CanExecuteAlbumDetail);
        }

        return albumDetail;
      }
    }

    public void OnAlbumDetail()
    {
      if (ActualItem is SongInPlayListViewModel songInPlay)
      {
        vPlayerRegionProvider.ShowAlbumDetail(songInPlay.AlbumViewModel);
      }

    }

    private bool CanExecuteAlbumDetail()
    {
      if (ActualItem is SongInPlayListViewModel songInPlay)
      {
        return songInPlay?.AlbumViewModel != null;
      }

      return false;
    }

    #endregion

    #region AddArtists

    private IEnumerable<SongInPlayListViewModel> GetArtistsToAdd()
    {
      return PlayList.OfType<SongInPlayListViewModel>()
        .Where(x => x.ArtistViewModel != null)
        .Where(x => x.ArtistViewModel.Model.Id == 0)
        .Where(x => !string.IsNullOrEmpty(x.ArtistViewModel.Name));
    }


    private ActionCommand addArtists;

    public ICommand AddArtists
    {
      get
      {
        if (addArtists == null)
        {
          addArtists = new ActionCommand(OnAddArtists, CanAddArtists);
        }

        return addArtists;
      }
    }

    private bool CanAddArtists()
    {
      if (WasAllItemsProcessed)
      {
        var validItems = GetArtistsToAdd()
          .GroupBy(x => x.ArtistViewModel.Model)
          .ToList();

        ArtistThatCanBeAddedCount = validItems.Count;

        return ArtistThatCanBeAddedCount > 0;
      }

      return false;
    }

    public void OnAddArtists()
    {
      Task.Run(async () =>
      {
        var validItems = GetArtistsToAdd();

        bool result = true;

        foreach (var song in validItems)
        {
          var storeResult = storageManager.StoreEntity(song.ArtistViewModel.Model, out var newVersion);

          result = result && storeResult;

          newVersion.Albums = song.ArtistViewModel.Model.Albums;

          foreach (var item in PlayList.OfType<SongInPlayListViewModel>()
            .Where(x => x.ArtistViewModel != null)
            .Where(x => x.ArtistViewModel.Model == song.ArtistViewModel.Model))
          {
            item.ArtistViewModel.Model = newVersion;
          }
        }


        VSynchronizationContext.PostOnUIThread(() =>
       {
         if (result)
         {
           statusManager.ShowDoneMessage($"Artists added");
         }
         else
         {
           statusManager.ShowFailedMessage($"Artists FAILED to add");
         }


         addArtists?.RaiseCanExecuteChanged();
       });
      });
    }

    #endregion

    #region AddAlbums

    private IEnumerable<SongInPlayListViewModel> GetAlbumsToAdd()
    {
      return PlayList.OfType<SongInPlayListViewModel>()
        .Where(x => x.AlbumViewModel != null)
        .Where(x => x.AlbumViewModel.Model.Id == 0)
        .Where(x => x.AlbumViewModel.Model.Artist != null)
        .Where(x => !string.IsNullOrEmpty(x.AlbumViewModel.Name))
        .Where(x => !string.IsNullOrEmpty(x.AlbumViewModel.Model.Artist.Name));
    }

    private ActionCommand addAlbums;

    public ICommand AddAlbums
    {
      get
      {
        if (addAlbums == null)
        {
          addAlbums = new ActionCommand(OnAddAlbums, () =>
          {
            if (WasAllItemsProcessed)
            {
              var validItems = GetAlbumsToAdd()
                .GroupBy(x => x.AlbumViewModel.Model)
                .ToList();

              AlbumsThatCanBeAddedCount = validItems.Count;

              return AlbumsThatCanBeAddedCount > 0;
            }

            return false;
          });
        }

        return addAlbums;
      }
    }

    public void OnAddAlbums()
    {
      Task.Run(async () =>
      {
        var validItems = GetAlbumsToAdd();

        bool result = true;

        foreach (var soundItemInPlaylistViewModel in validItems)
        {
          if (soundItemInPlaylistViewModel is SongInPlayListViewModel songVm)
          {
            var album = songVm.AlbumViewModel?.Model;

            if (album != null && album.Artist != null)
            {
              var artist = album.Artist;

              album.Songs.Where(x => x.ItemModel != null).Where(x => x.ItemModel.Id != 0).ForEach(x =>
              {
                x.ItemModelId = x.ItemModel.Id;
                x.ItemModel = null;
              });


              if (artist.Id != 0)
              {
                album.ArtistId = artist.Id;
              }
              else
              {
                storageManager.StoreEntity(artist, out var newArtist);
                album.ArtistId = newArtist.Id;
              }

              album.Artist = null;
              album.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

              var storeResult = storageManager.StoreAlbum(album, out var newVersion);

              newVersion.Artist = artist;
              result = result && storeResult;

              songVm.AlbumViewModel.Model = newVersion;

              foreach (var item in PlayList.OfType<SongInPlayListViewModel>()
                .Where(x => x.AlbumViewModel != null)
                .Where(x => x.AlbumViewModel.Model == album))
              {

                item.AlbumViewModel.Model = newVersion;
              }
            }
          }

          VSynchronizationContext.PostOnUIThread(() =>
          {
            if (result)
            {
              statusManager.ShowDoneMessage($"Albums added");
            }
            else
            {
              statusManager.ShowFailedMessage($"Albums FAILED to add");
            }

            addAlbums?.RaiseCanExecuteChanged();
            addArtists?.RaiseCanExecuteChanged();
          });
        }
      });

    }

    #endregion

    #region AddLyrics

    private ActionCommand<SoundItemInPlaylistViewModel> addLyrics;

    public ICommand AddLyrics
    {
      get
      {
        if (addLyrics == null)
        {
          addLyrics = new ActionCommand<SoundItemInPlaylistViewModel>(OnAddLyrics);
        }

        return addLyrics;
      }
    }

    public async void OnAddLyrics(SoundItemInPlaylistViewModel soundItemInPlaylistViewModel)
    {
      var vm = viewModelsFactory.Create<AddLyricsPromptViewModel>();

      if (soundItemInPlaylistViewModel is SongInPlayListViewModel songInPlayList)
      {
        vm.Lyrics = songInPlayList.Lyrics;
      }


      windowManager.ShowPrompt<AddLyricsPromptView>(vm);

      if (vm.PromptResult == PromptResult.Ok && soundItemInPlaylistViewModel is SongInPlayListViewModel song)
      {
        var lyrics = vm.Lyrics;
        song.Lyrics = lyrics;

        await storageManager.UpdateEntityAsync(song.SongModel);

        if (song.LRCCreatorViewModel != null && song.IsInEditMode)
          song.LRCCreatorViewModel.ChangeLyrics(song.Lyrics);

        song.RaiseLyricsChange();
      }
    }

    #endregion

    #region CreateLRCLyrics

    private ActionCommand<SoundItemInPlaylistViewModel> createLRCLyrics;

    public ICommand CreateLRCLyrics
    {
      get
      {
        if (createLRCLyrics == null)
        {
          createLRCLyrics = new ActionCommand<SoundItemInPlaylistViewModel>(OnCreateLRCLyrics);
        }

        return createLRCLyrics;
      }
    }

    public void OnCreateLRCLyrics(SoundItemInPlaylistViewModel soundItem)
    {
      if (soundItem is SongInPlayListViewModel song)
      {
        if (song.LyricsObject is LRCFileViewModel lrCFileViewModel)
        {
          var vm = viewModelsFactory.Create<LRCCreatorViewModel>(song, lrCFileViewModel);
          vm.LoadLines(lrCFileViewModel);
          vm.FilePlayableRegionViewModel = this;

          song.LRCCreatorViewModel = vm;
          song.IsInEditMode = true;

          if (song.Lyrics != vm.Lyrics)
          {
            vm.ChangeLyrics(song.Lyrics);
          }

          song.Lyrics = vm.Lyrics;

          SetMediaPosition(0);

          song.RaiseLyricsChange();
        }
        else if (!string.IsNullOrEmpty(song.Lyrics))
        {
          if (song.LRCCreatorViewModel == null)
          {
            var vm = viewModelsFactory.Create<LRCCreatorViewModel>(song);

            song.LRCCreatorViewModel = vm;

            vm.Lyrics = song.Lyrics;
            vm.FilePlayableRegionViewModel = this;

            vm.PrepareViewModel();
          }
          else
          {
            song.LRCCreatorViewModel.ChangeLyrics(song.Lyrics);
          }

          song.IsInEditMode = true;

          song.RaiseLyricsChange();

          PlayNextItemOnEndReached = false;
        }
      }
    }

    #endregion

    #region ExitLRCEditor

    private ActionCommand<SoundItemInPlaylistViewModel> exitLRCEditor;

    public ICommand ExitLRCEditor
    {
      get
      {
        if (exitLRCEditor == null)
        {
          exitLRCEditor = new ActionCommand<SoundItemInPlaylistViewModel>(OnExitLRCEditor);
        }

        return exitLRCEditor;
      }
    }

    public void OnExitLRCEditor(SoundItemInPlaylistViewModel soundItem)
    {
      if (soundItem is SongInPlayListViewModel song)
      {
        song.IsInEditMode = false;

        song.RaiseLyricsChange();

        PlayNextItemOnEndReached = true;
      }
    }

    #endregion

    #region PlayPauseUPnP

    private ActionCommand playPauseUPnP;

    public ICommand PlayPauseUPnP
    {
      get
      {
        if (playPauseUPnP == null)
        {
          playPauseUPnP = new ActionCommand(OnPlayPauseUPnP, CanExecutePlayPauseUPnP);
        }

        return playPauseUPnP;
      }
    }

    private bool CanExecutePlayPauseUPnP()
    {
      return SelectedMediaRendererViewModel != null;
    }

    private bool isUPnp;
    private MediaRendererPlayer mediaRendererPlayer;
    public async void OnPlayPauseUPnP()
    {
      try
      {
        if (!isUPnp)
        {
          BufferingSubject.OnNext(true);

          MediaPlayer.Stop();

          if (mediaRendererPlayer == null)
          {
            mediaRendererPlayer = new MediaRendererPlayer(SelectedMediaRendererViewModel.Model);

            MediaPlayer = mediaRendererPlayer;

            HookToPlayerEvents();
          }
          else
            MediaPlayer = mediaRendererPlayer;

          await SetMedia(ActualItem.Model);

          if (IsPlaying)
          {
            MediaPlayer.Play();
          }

          isUPnp = true;
        }
        else
        {
          mediaRendererPlayer?.Stop();

          await SetVlcPlayer();
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());

        await SetVlcPlayer();
      }
    }

    #endregion

    #region VideoViewInitlized

    private TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();
    private ActionCommand videoViewInitlized;

    public ICommand VideoViewInitlized
    {
      get
      {
        if (videoViewInitlized == null)
        {
          videoViewInitlized = new ActionCommand(OnVideoViewInitlized);
        }

        return videoViewInitlized;
      }
    }

    public void OnVideoViewInitlized()
    {
      if (!loadedTask.Task.IsCompleted)
        loadedTask.SetResult(true);
    }

    #endregion

    #region SaveVideo

    protected ActionCommand saveVideo;

    public ICommand SaveVideo
    {
      get
      {
        return saveVideo ??= new ActionCommand(OnSaveVideo);
      }
    }

    protected virtual void OnSaveVideo()
    {
      ActualItem.Model.VideoPath = LyricsConstants.Instance.VideoPath;
      UpdateActualSavedPlaylistPlaylist();
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    private LibVLC videolibVlc;
    private List<IDisposable> modelsDisposables = new List<IDisposable>();
    public override async void Initialize()
    {
      IsPlaying = false;

      InitializeAsync();

      ViewInitializedTask = new TaskCompletionSource<bool>();

      modelsDisposables.Add(storageManager.ObserveOnItemChange<Song>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSongChange).DisposeWith(this));
      modelsDisposables.Add(storageManager.ObserveOnItemChange<Album>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnAlbumChange).DisposeWith(this));
      modelsDisposables.Add(storageManager.ObserveOnItemChange<SoundItem>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSoundItemUpdated).DisposeWith(this));


      eventAggregator.GetEvent<ItemUpdatedEvent<AlbumViewModel>>().Subscribe(OnAlbumUpdated).DisposeWith(this);
      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Subscribe(RemoveFromPlaystSongs).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SongInPlayListViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Subscribe(PlaySongItems).DisposeWith(this);

      PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
      PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);


      UPnPManagerViewModel.Renderers.OnActualItemChanged
        .ObserveOn(Application.Current.Dispatcher)
        .Subscribe((x) => SelectedMediaRendererViewModel = x)
        .DisposeWith(this);

      //await artistsViewModel.GetViewModelsAsync();
      //await albumsViewModel.GetViewModelsAsync();

      videolibVlc = new LibVLC();
      VideoVLCPlayer = new MediaPlayer(videolibVlc);
      VideoVLCPlayer.Mute = true;
      VideoVLCPlayer.Volume = 0;

      MediaPlayer.Playing += MediaPlayer_Playing;
    }

    private void MediaPlayer_Playing(object sender, EventArgs e)
    {
      Task.Run(async () =>
      {
        if (!loadedTask.Task.IsCompleted)
          await loadedTask.Task;


      });

    }

    #endregion

    protected override void OnIsPlayingChanged()
    {
      base.OnIsPlayingChanged();
      LyricsConstants.Instance.RaiseConstants();
      if (ActualItem is SongInPlayListViewModel song)
      {
        song.UpdateSyncedLyrics();
      }
      VideoVLCPlayer.Pause();
    }

    #region OnSoundItemUpdated

    private async void OnSoundItemUpdated(IItemChanged<SoundItem> soundItemChanged)
    {
      if (soundItemChanged.Changed == Changed.Updated)
      {
        var insPlaylist = PlayList.Where(x => x.Model.Id == soundItemChanged.Item.Id);

        foreach (var inPlaylist in insPlaylist)
        {
          var lastIsAutomaticLyricsFindEnabled = inPlaylist.Model.IsAutomaticLyricsFindEnabled;

          inPlaylist.Model.Update(soundItemChanged.Item);

          inPlaylist.RaiseNotifyPropertyChanged(nameof(SoundItemInPlaylistViewModel.Model));

          if (inPlaylist is SongInPlayListViewModel song &&
              lastIsAutomaticLyricsFindEnabled != song.Model.IsAutomaticLyricsFindEnabled)
          {
            song.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.IsAutomaticLyricsDownloadDisabled));

            await song.TryToRefreshUpdateLyrics();
          }
        }
      }
    }

    #endregion

    #region PlaySongItems

    private void PlaySongItems(PlayItemsEventData<SongInPlayListViewModel> tvShowEpisodeInPlaylistViewModel)
    {
      var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(tvShowEpisodeInPlaylistViewModel.Items, tvShowEpisodeInPlaylistViewModel.EventAction, tvShowEpisodeInPlaylistViewModel.Model);

      PlayItemsFromEvent(data);
    }

    #endregion

    #region RemoveFromPlaystSongs

    private void RemoveFromPlaystSongs(RemoveFromPlaylistEventArgs<SongInPlayListViewModel> obj)
    {
      var data = new RemoveFromPlaylistEventArgs<SoundItemInPlaylistViewModel>()
      {
        DeleteType = obj.DeleteType,
        ItemsToRemove = obj.ItemsToRemove.Select(x => (SoundItemInPlaylistViewModel)x).ToList()
      };

      RemoveItemsFromPlaylist(data);
    }

    #endregion

    #region OnActivation

    TaskCompletionSource<bool> ViewInitializedTask = new TaskCompletionSource<bool>();
    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        regionProvider.RegisterView<SongPlayerView, MusicPlayerViewModel>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }

      if (!ViewInitializedTask.Task.IsCompleted)
      {
        await Task.Delay(1000);
        ViewInitializedTask.SetResult(true);
      }
       
    }

    #endregion

    #region WaitForInitilization

    protected override Task WaitForVlcInitilization()
    {
      return loadedTask.Task;
    }

    #endregion

    #region OnActualItemChanged

    protected override void OnActualItemChanged()
    {
      VSynchronizationContext.PostOnUIThread(async () =>
      {
        base.OnActualItemChanged();

        addArtists?.RaiseCanExecuteChanged();
        addAlbums?.RaiseCanExecuteChanged();

        await SetPlaylistCover();

        albumDetail?.RaiseCanExecuteChanged();

        await PlayVideo();

        LyricsConstants.Instance.RaiseConstants();
      });
    }

    #endregion


    private SemaphoreSlim semaphoreVideoSlim = new SemaphoreSlim(1, 1);
    private async Task PlayVideo()
    {
      if (!await semaphoreVideoSlim.WaitAsync(0))
        return;

      try
      {
        await ViewInitializedTask.Task;

        var videoPath = LyricsConstants.Instance.VideoPath ?? ActualItem.Model.VideoPath;
        var shouldPlay = LyricsConstants.Instance.IsVideo || !string.IsNullOrEmpty(ActualItem.Model.VideoPath);

        if (shouldPlay && !string.IsNullOrEmpty(videoPath))
        {
          VideoVLCPlayer.Play(new Media(videolibVlc, videoPath, FromType.FromPath, "--loop"));
          LyricsConstants.Instance.IsVideo = true;

          if (!IsPlaying)
          {
            await Task.Delay(1000);
            VideoVLCPlayer.Pause();
          }

        }
        else
        {
          Task.Run(() =>
          {
            VideoVLCPlayer.Stop();
            VideoVLCPlayer.Media = null;
            LyricsConstants.Instance.IsVideo = false;
          });
       
        }
      }
      finally
      {
        semaphoreVideoSlim.Release();
      }
    }

    #region SetPlaylistCover

    private async Task SetPlaylistCover()
    {
      if (ActualItem is SongInPlayListViewModel song &&
          song.AlbumViewModel?.Image != null &&
          ActualSavedPlaylist.CoverPath != song.AlbumViewModel.Image &&
          song.AlbumViewModel.Image != PlayableViewModelWithThumbnail<SongInPlayListViewModel, Album>.GetEmptyImage()
          && File.Exists(song.AlbumViewModel.Image))
      {
        ActualSavedPlaylist.CoverPath = song.AlbumViewModel.Image;
        await UpdateActualSavedPlaylistPlaylist();
      }
    }

    #endregion

    #region OnNewItemPlay

    public override void OnNewItemPlay(SoundItem soundItem)
    {
      base.OnNewItemPlay(soundItem);
    }

    #endregion

    #region GetValidItemsForCloud

    protected override IEnumerable<SoundItemInPlaylistViewModel> GetValidItemsForCloud(IEnumerable<SoundItemInPlaylistViewModel> validItems)
    {
      return validItems
        .OfType<SongInPlayListViewModel>()
        .Where(x => x.ArtistViewModel != null &&
                    x.AlbumViewModel != null).ToList();
    }

    #endregion

    #region DownloadItemInfo

    protected override async Task DownloadItemInfo(CancellationToken cancellationToken)
    {
      var list = PlayList.OfType<SongInPlayListViewModel>().ToList();

      await Task.Run(async () =>
      {
        await base.DownloadItemInfo(cancellationToken);


        await DownloadHighQualityAlbumCover(ActualItem);
      });


      await SetPlaylistCover();
    }

    #endregion

    #region DownloadHighQualityAlbumCover

    private SemaphoreSlim downloadHighQualityAlbumCoverSempahore = new SemaphoreSlim(1, 1);
    private Task DownloadHighQualityAlbumCover(SoundItemInPlaylistViewModel item)
    {
      return Task.Run(async () =>
      {
        try
        {
          await downloadHighQualityAlbumCoverSempahore.WaitAsync();

          if (item is SongInPlayListViewModel songInPlay)
          {
            var artistName = songInPlay?.ArtistViewModel?.Name;
            var albumName = songInPlay?.AlbumViewModel?.Name;
            var albumViewModel = songInPlay.AlbumViewModel;
            var finalPath = albumViewModel?.Model.AlbumFrontCoverFilePath;

            if (!string.IsNullOrEmpty(artistName) &&
                !string.IsNullOrEmpty(albumName) &&
                (!File.Exists(songInPlay.AlbumViewModel.ImageThumbnail)
                 || PlayableViewModelWithThumbnail<SongInPlayListViewModel, Album>.GetEmptyImage() == songInPlay.AlbumViewModel.ImageThumbnail))
            {
              var cover = await iPCloudAlbumCoverProvider.GetAlbumCover(artistName, albumName);

              if (cover == null || IsEmptyImage(cover))
              {
                var coverPath = songInPlay?.AlbumViewModel?.Image;
                byte[] actualCover = null;

                if (File.Exists(coverPath))
                {
                  actualCover = File.ReadAllBytes(coverPath);

                  if (IsEmptyImage(actualCover))
                  {
                    File.Delete(coverPath);
                    actualCover = null;
                  }
                  else
                  {
                    cover = actualCover;
                  }
                }



                if (actualCover == null)
                {
                  var directory = Path.GetDirectoryName(songInPlay.Model.Source);

                  var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

                  List<System.IO.FileInfo> imageFiles = new List<System.IO.FileInfo>();

                  foreach (string filename in files)
                  {
                    if (Regex.IsMatch(filename, @"\.jpg$|\.png$|\.gif$"))
                      imageFiles.Add(new System.IO.FileInfo(filename));
                  }

                  if (imageFiles.Count > 0)
                  {
                    actualCover = File.ReadAllBytes(imageFiles.OrderByDescending(x => x.Length).First().FullName);

                    if (songInPlay?.AlbumViewModel?.Model != null && string.IsNullOrEmpty(songInPlay?.AlbumViewModel?.Model.AlbumFrontCoverFilePath))
                    {
                      songInPlay.AlbumViewModel.Model.AlbumFrontCoverFilePath = imageFiles.OrderByDescending(x => x.Length).First().FullName;
                    }
                  }

                }

                if (!string.IsNullOrEmpty(albumViewModel?.Model?.AlbumFrontCoverURI) && actualCover == null)
                {
                  using (var vc = new WebClient())
                  {
                    actualCover = vc.DownloadData(albumViewModel.Model.AlbumFrontCoverURI);
                    finalPath = await SaveAlbumCover(actualCover, albumViewModel);

                    if (IsEmptyImage(actualCover))
                    {
                      actualCover = null;
                    }
                    else
                    {
                      cover = actualCover;
                    }
                  }
                }

                if (actualCover != null)
                {
                  var result = await iPCloudAlbumCoverProvider.UpdateOrCreateAlbumCover(artistName, albumName, actualCover);

                  if (result)
                  {
                    songInPlay.AlbumViewModel.HighQualityCover = actualCover;
                  }
                }
              }
              else
              {
                finalPath = await SaveAlbumCover(cover, albumViewModel);
              }

              CacheImageConverter.RefreshDictionary(finalPath);

              songInPlay.AlbumViewModel.HighQualityCover = cover;

              albumViewModel.RaisePropertyChange(nameof(AlbumViewModel.Image));
              albumViewModel.RaisePropertyChange(nameof(AlbumViewModel.ImageThumbnail));
              albumViewModel.RaisePropertyChange(nameof(AlbumViewModel.HighQualityCover));
              ActualItem?.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.ImagePath));

              await storageManager.UpdateEntityAsync(songInPlay.AlbumViewModel.Model);
            }
          }
        }
        catch (Exception ex)
        {

        }
        finally
        {
          downloadHighQualityAlbumCoverSempahore.Release();
        }
      });
    }

    #endregion

    #region SaveAlbumCover

    private async Task<string> SaveAlbumCover(byte[] cover, AlbumViewModel albumViewModel)
    {
      var filePath = Path.Combine(AudioInfoDownloader.GetAlbumCoverImagePath(albumViewModel.Model));

      if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath))
      {
        MemoryStream ms = new MemoryStream(cover);
        Image i = Image.FromStream(ms);
        byte[] acutalFile = null;

        if (File.Exists(filePath))
          acutalFile = File.ReadAllBytes(filePath);


        await storageManager.UpdateEntityAsync(albumViewModel.Model);

        if (acutalFile != cover)
        {
          filePath.EnsureDirectoryExists();

          if (File.Exists(filePath))
          {
            File.Delete(filePath);
          }

          i.Save(filePath, ImageFormat.Jpeg);

          ms?.Dispose();
          i?.Dispose();
        }
      }

      albumViewModel.Model.AlbumFrontCoverFilePath = filePath;

      return filePath;
    }

    #endregion

    #region IsEmptyImage

    private bool IsEmptyImage(byte[] cover)
    {
      return cover.Length == 631 && cover[0] == 255 && cover[1] == 216;
    }

    #endregion

    #region DownloadSongInfo

    private string originalDownlaodedArtistName;
    private string originalDownlaodedAlbumName;


    private Artist downloadingArtist;
    private Album downloadingAlbum;

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    private async Task DownloadSongInfo(SoundItemInPlaylistViewModel viewmodel, CancellationToken cancellationToken)
    {
      if (viewmodel == null)
      {
        return;
      }

      if (viewmodel is SongInPlayListViewModel songInPlayListViewModel &&
          songInPlayListViewModel.AlbumViewModel == null &&
          songInPlayListViewModel.ArtistViewModel == null)
      {
        try
        {
          await semaphoreSlim.WaitAsync();
          cancellationToken.ThrowIfCancellationRequested();

          if (CheckedFiles.Contains(viewmodel))
          {
            RequestUIDispatcher(() => { CheckedFiles.Remove(viewmodel); });
          }

          var fileInfo = viewmodel?.Model?.FileInfoEntity;
          bool fileInfoWasChanged = false;

          RequestUIDispatcher(() => { viewmodel.IsDownloading = true; });

          if (fileInfo != null)
          {
            string artistName = viewmodel?.Model?.FileInfoEntity.Artist;
            string albumName = viewmodel?.Model?.FileInfoEntity.Album;

            if (string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(albumName))
            {
              if (long.TryParse(fileInfo.Indentificator, out var id))
              {
                var stats = (await cloudService.GetFileStats(id));

                var data = stats?.metadata;

                if (data != null)
                {
                  fileInfo.Artist = AudioInfoDownloader.GetClearName(data.artist);
                  fileInfo.Album = AudioInfoDownloader.GetClearName(data.album);

                  if (artistName != fileInfo.Artist || fileInfo.Album != albumName)
                  {
                    fileInfoWasChanged = true;
                  }

                  if (!string.IsNullOrEmpty(data.title))
                  {
                    var name = AudioInfoDownloader.GetClearName(data.title);

                    if (name != fileInfo.Name)
                    {
                      fileInfoWasChanged = true;
                    }

                    fileInfo.Name = name;
                  }
                }
              }
              else if (!fileInfo.Source.Contains("http"))
              {
                var info = await Task.Run(() => audioInfoDownloader.GetAudioInfoByWindowsAsync(fileInfo.Source));

                fileInfo.Artist = AudioInfoDownloader.GetClearName(info?.Artist);
                fileInfo.Album = AudioInfoDownloader.GetClearName(info?.Album);

                if (artistName != fileInfo.Artist || fileInfo.Album != albumName)
                {
                  fileInfoWasChanged = true;
                }

                if (!string.IsNullOrEmpty(info?.Title))
                {
                  var name = AudioInfoDownloader.GetClearName(info.Title);

                  if (name != fileInfo.Name)
                  {
                    fileInfoWasChanged = true;
                  }

                  fileInfo.Name = name;
                }
              }
            }

            artistName = fileInfo.Artist;
            albumName = fileInfo.Album;
            viewmodel.Model.FileInfoEntity.Name = fileInfo.Name;

            cancellationToken.ThrowIfCancellationRequested();

            if (fileInfoWasChanged)
            {
              await storageManager.UpdateEntityAsync(fileInfo);
            }

            var downloadArtist = true;
            var downloadAlbum = true;

            if (!string.IsNullOrEmpty(artistName))
            {
              var normalizedArtistName = StringHelper.GetNormalizedName(artistName);
              string normalizedDownloadingAristName = null;

              if (downloadingArtist != null)
              {
                normalizedDownloadingAristName = StringHelper.GetNormalizedName(downloadingArtist.Name);
              }

              if (downloadingArtist == null || string.IsNullOrEmpty(downloadingArtist.Name))
              {
                downloadingArtist = storageManager.GetTempRepository<Artist>().FirstOrDefault(x => x.NormalizedName == normalizedArtistName);

                if (downloadingArtist == null && !string.IsNullOrEmpty(normalizedArtistName))
                {
                  var existingArtist = GetExistingArtist(normalizedArtistName);

                  if (existingArtist != null)
                  {
                    downloadingArtist = existingArtist;
                  }
                }

                if (downloadingArtist != null)
                {
                  downloadArtist = false;
                }
              }


              if ((downloadingArtist == null || (!string.IsNullOrEmpty(normalizedArtistName) &&
                                                 normalizedArtistName != normalizedDownloadingAristName)) &&
                  originalDownlaodedArtistName != artistName && downloadArtist)
              {
                downloadingArtist = storageManager.GetTempRepository<Artist>().FirstOrDefault(x => x.NormalizedName == StringHelper.GetNormalizedName(artistName));

                if (downloadingArtist == null)
                {
                  downloadingArtist = await GetArtist(artistName, cancellationToken);

                  if (downloadingArtist != null)
                  {
                    originalDownlaodedArtistName = artistName;
                    var similiarity = StringHelper.GetNormalizedName(artistName).Similarity(StringHelper.GetNormalizedName(downloadingArtist.Name));

                    if (similiarity < 0.8)
                    {
                      downloadingArtist = null;
                    }
                  }


                  if (downloadingArtist != null && !string.IsNullOrEmpty(downloadingArtist.NormalizedName))
                  {
                    var existingArtist = GetExistingArtist(downloadingArtist.NormalizedName);

                    if (existingArtist == null)
                    {
                      existingArtist = storageManager.GetTempRepository<Artist>().FirstOrDefault(x => x.NormalizedName == StringHelper.GetNormalizedName(downloadingArtist.Name));
                    }

                    if (existingArtist != null)
                    {
                      downloadingArtist = existingArtist;
                    }
                  }
                }

              }
            }
            else
            {
              downloadingArtist = null;
              originalDownlaodedArtistName = null;
            }


            if (!string.IsNullOrEmpty(albumName))
            {
              var normalizedName = StringHelper.GetNormalizedName(albumName);

              string normalizedDownloadingAlbumName = null;

              if (downloadingAlbum != null)
                normalizedDownloadingAlbumName = StringHelper.GetNormalizedName(downloadingAlbum.Name);

              if (downloadingAlbum == null || string.IsNullOrEmpty(downloadingAlbum.Name))
              {
                downloadingAlbum = GetBestFittingAlbumFromDb(StringHelper.GetNormalizedName(normalizedName), downloadingArtist?.Name, downloadingArtist?.Id);

                if (downloadingAlbum?.Name?.Similarity(albumName) < 0.8)
                {
                  downloadingAlbum = null;
                }

                if (downloadingAlbum == null && !string.IsNullOrEmpty(normalizedName))
                {
                  var existingAlbum = GetExistingAlbum(normalizedName, downloadingArtist?.Name);

                  if (existingAlbum != null)
                  {
                    downloadingAlbum = existingAlbum;
                  }
                }

                if (downloadingArtist == null)
                {
                  downloadAlbum = false;
                }
              }


              if ((downloadingAlbum == null ||
                   (!string.IsNullOrEmpty(normalizedDownloadingAlbumName) &&
                    normalizedName != normalizedDownloadingAlbumName)) &&
                  originalDownlaodedAlbumName != albumName && downloadAlbum)
              {

                downloadingAlbum = GetBestFittingAlbumFromDb(StringHelper.GetNormalizedName(albumName), downloadingArtist?.Name);

                if (downloadingAlbum == null)
                {
                  downloadingAlbum = await GetAlbum(downloadingArtist, albumName, cancellationToken);

                  if (downloadingAlbum?.Name?.Similarity(albumName) < 0.8)
                  {
                    downloadingAlbum = null;
                  }

                  if (downloadingAlbum != null && !string.IsNullOrEmpty(downloadingAlbum.NormalizedName))
                  {
                    var existingAlbum = GetExistingAlbum(downloadingAlbum.Name, downloadingArtist?.Name);

                    if (existingAlbum == null)
                    {
                      existingAlbum = GetBestFittingAlbumFromDb(StringHelper.GetNormalizedName(downloadingAlbum.Name), downloadingArtist?.Name);
                    }

                    if (existingAlbum != null)
                    {
                      downloadingAlbum = existingAlbum;
                    }
                  }

                  originalDownlaodedAlbumName = albumName;
                }
              }
            }
            else
            {
              downloadingAlbum = null;
              originalDownlaodedAlbumName = null;
            }

            if (downloadingArtist != null)
            {
              if (downloadingAlbum == null)
              {
                downloadingAlbum = GetExistingAlbum(albumName, downloadingArtist?.Name);

                if (downloadingAlbum == null)
                {
                  downloadingAlbum = new Album()
                  {
                    Artist = downloadingArtist,
                    Name = albumName,
                    NormalizedName = StringHelper.GetNormalizedName(albumName)
                  };
                }
              }
            }
            else
            {
              downloadingArtist = GetExistingArtist(artistName);

              if (downloadingArtist == null)
              {
                downloadingArtist = new Artist()
                {
                  Name = artistName,
                  NormalizedName = StringHelper.GetNormalizedName(artistName)
                };
              }

              if (downloadingAlbum == null)
              {
                downloadingAlbum = GetExistingAlbum(albumName, downloadingArtist?.Name);

                if (downloadingAlbum == null)
                {
                  downloadingAlbum = new Album()
                  {
                    Artist = downloadingArtist,
                    Name = albumName,
                    NormalizedName = StringHelper.GetNormalizedName(albumName)
                  };
                }
              }
            }

            bool wasChanged = false;


            if (!string.IsNullOrEmpty(downloadingAlbum.Name) && downloadingAlbum.Name != fileInfo.Album)
            {
              fileInfo.Album = downloadingAlbum.Name;
              wasChanged = true;
            }

            if (!string.IsNullOrEmpty(downloadingArtist.Name) && downloadingArtist.Name != fileInfo.Artist)
            {
              fileInfo.Artist = downloadingArtist.Name;
              wasChanged = true;
            }

            if (wasChanged)
              await storageManager.UpdateEntityAsync(fileInfo);

            songInPlayListViewModel.SongModel.Album = downloadingAlbum;

            if (downloadingArtist != null && songInPlayListViewModel.SongModel.Album != null)
            {
              if ((songInPlayListViewModel.SongModel.Album.ArtistId == downloadingArtist.Id || downloadingArtist.Id == 0)
                  && songInPlayListViewModel.SongModel.Album.Artist == null)
              {
                songInPlayListViewModel.SongModel.Album.Artist = downloadingArtist;
              }
            }

            songInPlayListViewModel.Initialize();

            if (downloadingAlbum.Id > 0 && downloadingArtist.Id > 0)
            {
              var song = downloadingAlbum.Songs
                .FirstOrDefault(x => x.ItemModel.FileInfoEntity.Name == songInPlayListViewModel.SongModel.ItemModel.FileInfoEntity.Name);

              if (song == null)
              {
                song = downloadingAlbum.Songs
                  .FirstOrDefault(x => x.Name == songInPlayListViewModel.SongModel.Name);
              }

              if (song != null && songInPlayListViewModel.SongModel.Id != song.Id)
              {
                //This was replacing song model with old one in the db.
                //as consequence Source was not updated and FileInfoEntity was wrong

                //Consider migrate timeplayed to count as one song not 2 different files
                //songInPlayListViewModel.Model.TimePlayed =




                //var source = songInPlayListViewModel.SongModel.Source;

                //songInPlayListViewModel.SongModel = song;
                //songInPlayListViewModel.Model = song.ItemModel;

                // song.ItemModel.Source = source;
              }
            }

            if (songInPlayListViewModel.AlbumViewModel == null && songInPlayListViewModel.SongModel.Album != null)
            {
              var vm = PlayList.OfType<SongInPlayListViewModel>()
                .Where(x => x.AlbumViewModel != null)
                .FirstOrDefault(x => x.AlbumViewModel.Model == downloadingAlbum)?.AlbumViewModel;

              if (vm == null)
              {
                vm = viewModelsFactory.Create<AlbumViewModel>(downloadingAlbum);
              }

              songInPlayListViewModel.AlbumViewModel = vm;
            }

            if (songInPlayListViewModel.ArtistViewModel == null && songInPlayListViewModel.SongModel.Album?.Artist != null)
            {
              var vm = PlayList.OfType<SongInPlayListViewModel>()
                .Where(x => x.ArtistViewModel != null)
                .FirstOrDefault(x => x.ArtistViewModel.Model == downloadingArtist)?.ArtistViewModel;

              if (vm == null)
              {
                vm = viewModelsFactory.Create<ArtistViewModel>(downloadingArtist);
              }

              songInPlayListViewModel.ArtistViewModel = vm;
            }

            if (string.IsNullOrEmpty(downloadingAlbum?.Name))
            {
              downloadingAlbum = null;
            }

            if (string.IsNullOrEmpty(downloadingArtist?.Name))
            {
              downloadingArtist = null;
            }

            RequestUIDispatcher(async () =>
            {
              try
              {
                songInPlayListViewModel.SongModel.Album?.Songs.Add(songInPlayListViewModel.SongModel);
                songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SoundItemFilePlaylist.Name));
                songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.AlbumViewModel));
                songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.ArtistViewModel));

                var tmpSongModel = songInPlayListViewModel.SongModel;
                songInPlayListViewModel.SongModel = null;
                songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.SongModel));
                songInPlayListViewModel.SongModel = tmpSongModel;
                songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.SongModel));
                albumDetail?.RaiseCanExecuteChanged();

                if (songInPlayListViewModel == ActualItem)
                {
                  cancellationToken.ThrowIfCancellationRequested();
                  await DownloadHighQualityAlbumCover(ActualItem);

                  RaisePropertyChanged(nameof(ActualItem));
                  songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.ImagePath));
                }

                addArtists?.RaiseCanExecuteChanged();
                addAlbums?.RaiseCanExecuteChanged();

                MarkViewModelAsChecked(viewmodel);
                await SetPlaylistCover();

                cancellationToken.ThrowIfCancellationRequested();
                await songInPlayListViewModel.TryToRefreshUpdateLyrics();

              }
              catch (TaskCanceledException)
              {
                ResetDownload();
              }
              catch (OperationCanceledException)
              {
                ResetDownload();
              }
              catch (Exception ex)
              {
                logger.Log(ex);
              }
            });
          }
        }
        catch (TaskCanceledException)
        {
          ResetDownload();
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
        finally
        {
          RequestUIDispatcher(() =>
          {
            viewmodel.IsDownloading = false;
          });

          semaphoreSlim.Release();
        }
      }
      else
      {
        MarkViewModelAsChecked(viewmodel);
      }
    }

    #endregion

    #region GetBestFittingAlbumFromDb

    private Album GetBestFittingAlbumFromDb(string albumName, string artistName, int? artistId = null)
    {
      IEnumerable<Album> albums = new List<Album>();

      if (artistId != null)
      {
        albums = storageManager.GetTempRepository<Album>()
          .Where(x => x.NormalizedName == StringHelper.GetNormalizedName(albumName))
          .Where(x => x.ArtistId == artistId)
          .Include(x => x.Artist)
          .Include(x => x.Songs)
          .ThenInclude(x => x.ItemModel.FileInfoEntity)
          .ToList();
      }
      else
      {
        albums = storageManager.GetTempRepository<Album>()
          .Where(x => x.NormalizedName == StringHelper.GetNormalizedName(albumName))
          .Include(x => x.Artist)
          .Include(x => x.Songs)
          .ThenInclude(x => x.ItemModel.FileInfoEntity)
          .ToList();
      }


      Album result = albums.FirstOrDefault(x => StringHelper.GetNormalizedName(x.Artist.Name) == StringHelper.GetNormalizedName(artistName));

      return result;
    }

    #endregion

    #region GetExistingArtist

    public Artist GetExistingArtist(string name)
    {
      return PlayList.OfType<SongInPlayListViewModel>()
        .Where(x => x.ArtistViewModel != null)
        .GroupBy(x => x.ArtistViewModel.Model)
        .SingleOrDefault(x => x.Key.NormalizedName == StringHelper.GetNormalizedName(name))?.Key;
    }

    #endregion

    #region GetExistingAlbum

    public Album GetExistingAlbum(string name, string artistName)
    {
      if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(artistName))
      {
        return PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.AlbumViewModel != null)
          .Where(x => x.ArtistViewModel != null)
          .Where(x => x.ArtistViewModel.Name == artistName)
          .GroupBy(x => x.AlbumViewModel.Model)
          .SingleOrDefault(x => x.Key.NormalizedName == StringHelper.GetNormalizedName(name))?.Key;
      }

      return null;
    }

    #endregion

    #region GetArtist

    private async Task<Artist> GetArtist(string artistName, CancellationToken cancellationToken)
    {
      Artist artist = null;

      if (!string.IsNullOrEmpty(artistName))
      {
        cancellationToken.ThrowIfCancellationRequested();

        artist = await audioInfoDownloader.GetArtist(artistName);
      }

      return artist;
    }

    #endregion

    #region GetAlbum

    private async Task<Album> GetAlbum(Artist artist, string albumName, CancellationToken cancellationToken)
    {
      Album album = null;

      if (artist != null)
      {
        {
          var normalizedName = StringHelper.GetNormalizedName(albumName);

          cancellationToken.ThrowIfCancellationRequested();

          album = artist.Albums?.SingleOrDefault(x => x.NormalizedName == normalizedName);

          if (album == null)
          {
            album = new Album()
            {
              Artist = artist,
              Name = albumName,
              NormalizedName = StringHelper.GetNormalizedName(albumName)
            };

            cancellationToken.ThrowIfCancellationRequested();

            album = await audioInfoDownloader.UpdateAlbum(album);

            if (album != null)
            {
              var id = album.MusicBrainzId;
              var storedAlbum = artist.Albums?.SingleOrDefault(x => x.MusicBrainzId == id);

              if (storedAlbum != null)
              {
                album = storedAlbum;
              }
            }
          }
        }
      }

      return album;
    }

    #endregion

    #region DownloadLyrics

    private Task DownloadAllLyrics(IEnumerable<SongInPlayListViewModel> items, CancellationToken cancellationToken)
    {
      return Task.Run(async () =>
       {
         try
         {
           if (ActualItem is SongInPlayListViewModel songInPlay)
           {
             bool wasLyricsNull = songInPlay.LRCLyrics == null && songInPlay.Lyrics == null;

             var validItemsToUpdate = items.ToList();

             if (string.IsNullOrEmpty(songInPlay.LRCLyrics) && string.IsNullOrEmpty(songInPlay.Lyrics))
             {
               cancellationToken.ThrowIfCancellationRequested();

               await songInPlay.TryToRefreshUpdateLyrics();
             }

             if (songInPlay.LRCLyrics != null && wasLyricsNull)
             {
               cancellationToken.ThrowIfCancellationRequested();

               await storageManager.UpdateEntityAsync(ActualItem.Model);
             }

             var itemsAfter = validItemsToUpdate.Skip(actualItemIndex).Where(x => x.LyricsObject == null).ToList();
             var itemsBefore = validItemsToUpdate.Take(actualItemIndex).Where(x => x.LyricsObject == null).ToList();

             await DownloadLyrics(itemsAfter, cancellationToken);
             await DownloadLyrics(itemsBefore, cancellationToken);
           }
         }
         catch (TaskCanceledException)
         {
         }
         catch (OperationCanceledException)
         {
         }


       }, cancellationToken);
    }

    private async Task DownloadLyrics(IEnumerable<SongInPlayListViewModel> songInPlayListViewModels, CancellationToken cancellationToken)
    {
      var list = songInPlayListViewModels.ToList();

      foreach (var item in list)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var updated = await item.TryToRefreshUpdateLyrics();

        if (updated)
        {
          cancellationToken.ThrowIfCancellationRequested();

          await storageManager.UpdateEntityAsync(item.Model);
        }
      }
    }

    #endregion

    #region OnSetActualItem

    public override void OnSetActualItem(SoundItemInPlaylistViewModel itemViewModel, bool isPlaying)
    {
      if (itemViewModel is SongInPlayListViewModel viewModel)
      {
        if (viewModel.AlbumViewModel != null)
          viewModel.AlbumViewModel.IsPlaying = isPlaying;

        if (viewModel.ArtistViewModel != null)
          viewModel.ArtistViewModel.IsPlaying = isPlaying;
      }

    }

    #endregion

    #region GetNewPlaylistItemViewModel

    protected override PlaylistSoundItem GetNewPlaylistItemViewModel(SoundItemInPlaylistViewModel song, int index)
    {
      var playlistVideoItem = new PlaylistSoundItem();

      if (song.Model.Id != 0)
      {
        playlistVideoItem.IdReferencedItem = song.Model.Id;
      }
      else
      {
        return null;
      }

      return playlistVideoItem;
    }

    #endregion

    #region OnAlbumChange

    private void OnAlbumChange(IItemChanged<Album> change)
    {
      var album = change.Item;

      var songsInPlaylist = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null && x.AlbumViewModel.ModelId == album.Id);

      foreach (var song in songsInPlaylist)
      {
        song.UpdateAlbumViewModel(album);
      }

    }

    #endregion

    #region OnSongChange

    private void OnSongChange(IItemChanged<Song> itemChanged)
    {
      var song = itemChanged.Item;

      var playlistSong = PlayList.OfType<SongInPlayListViewModel>().SingleOrDefault(x => x.Model.Id == song.Id);

      if (playlistSong != null)
      {
        playlistSong.Update(song);

        if (ActualItem != null && ActualItem.Model.Id == song.Id && ActualItem is SongInPlayListViewModel inPlayListViewModel)
        {
          inPlayListViewModel.Update(song);
        }
      }
    }

    #endregion

    #region OnRemoveItemsFromPlaylist

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<SoundItemInPlaylistViewModel> args)
    {
      if (deleteType == DeleteType.AlbumFromPlaylist)
      {
        var album = args.ItemsToRemove.OfType<SongInPlayListViewModel>().FirstOrDefault(x => x.AlbumViewModel != null)?.AlbumViewModel;

        if (album != null && !string.IsNullOrEmpty(album.Name))
        {
          var albumSongs = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel == album).ToList();

          if (albumSongs.Count == 0)
          {
            albumSongs = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.SongModel.Album.Id == album.Model.Id).ToList();
          }

          foreach (var albumSong in albumSongs)
          {
            PlayList.Remove(albumSong);
          }
        }
        else
        {
          foreach (var item in args.ItemsToRemove)
          {
            PlayList.Remove(item);
          }
        }
      }
      else
      {
        foreach (var item in args.ItemsToRemove)
        {
          PlayList.Remove(item);
        }
      }
    }

    #endregion

    #region BeforePlayEvent

    protected override async Task BeforePlayEvent(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      await base.BeforePlayEvent(data);

      VSynchronizationContext.InvokeOnDispatcher(() =>
      {
        WasAllItemsProcessed = false;
        CheckedFiles.Clear();
      });

      modelsDisposables.ForEach(x => x.Dispose());
      modelsDisposables.Clear();

      downloadingSongTasks.Where(x => !x.IsCancellationRequested).ForEach(x => x.Cancel());
      downloadingSongTasks.Clear();

      var actualDownloadingSongTask = new CancellationTokenSource();

      downloadingSongTasks.Add(actualDownloadingSongTask);

      var songs = new List<SongInPlayListViewModel>();

      foreach (var item in data.Items)
      {
        var song = new Song()
        {
          ItemModel = item.Model
        };

        var vm = viewModelsFactory.Create<SongInPlayListViewModel>(song);

        vm.ActualPosition = item.ActualPosition;
        vm.IsFavorite = item.IsFavorite;
        vm.IsPlaying = item.IsPlaying;
        vm.IsSelected = item.IsSelected;
        vm.Duration = item.Duration;

        songs.Add(vm);
      }

      data.Items = songs;

      ResetDownload();

    }

    #endregion

    protected override IEnumerable<SoundItemInPlaylistViewModel> GetVmToPlayFromPlaylist(IEnumerable<PlaylistSoundItem> playlistItems)
    {
      return playlistItems.Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(new Song()
      {
        ItemModel = x.ReferencedItem
      }));
    }

    #region OnPlayPlaylist

    protected override void OnPlayPlaylist(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      base.OnPlayPlaylist(data);

      if (ActualSavedPlaylist.PlaylistType != PlaylistType.Cloud && data.Items.Select(x => x.Model.Source).Where(y => y != null).Any(y => y.Contains("http")))
      {
        ActualSavedPlaylist.PlaylistType = PlaylistType.Cloud;
      }
    }

    #endregion

    #region OnPlayEvent

    private List<CancellationTokenSource> downloadingSongTasks = new List<CancellationTokenSource>();

    protected override async void OnPlayEvent(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {

      base.OnPlayEvent(data);

      var playlistItems = data.Items.ToList();
      bool changed = false;

      await Task.Run(() =>
      {
        var songsItems = storageManager.GetTempRepository<Song>()
          .Where(x => playlistItems.Select(y => y.Model.Id).Contains(x.ItemModel.Id))
          .Include(x => x.Album)
          .ThenInclude(x => x.Artist)
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfoEntity)
          .ToList();

        if (songsItems.Count > 0)
        {

          PlayList.DisableNotification();
          UnHookToPlaylistCollectionChanged();

          foreach (var songItem in songsItems)
          {
            var index = PlayList.IndexOf(x => x.Model.Id == songItem.ItemModel.Id);

            if (index != null)
            {
              var itemInPlaylistViewModel = PlayList[index.Value];
              songItem.ItemModel = itemInPlaylistViewModel.Model;
              var vm = viewModelsFactory.Create<SongInPlayListViewModel>(songItem);

              vm.ActualPosition = itemInPlaylistViewModel.ActualPosition;
              vm.IsFavorite = itemInPlaylistViewModel.IsFavorite;
              vm.IsPlaying = itemInPlaylistViewModel.IsPlaying;
              vm.IsSelected = itemInPlaylistViewModel.IsSelected;
              vm.Duration = itemInPlaylistViewModel.Duration;

              if (songItem.Source != itemInPlaylistViewModel.Model.Source)
              {
                songItem.ItemModel.Source = itemInPlaylistViewModel.Model.Source;
              }


              PlayList[index.Value] = vm;

              if (itemInPlaylistViewModel == ActualItem)
              {
                ActualItem = vm;
              }
              changed = true;
            }
          }

          HookToPlaylistCollectionChanged();
          PlayList.EnableNotification();

        }
      });

      if (changed)
        RequestReloadVirtulizedPlaylist();

      if (PlayList.Count > 0)
      {
        var allArtists = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).GroupBy(x => x.ArtistViewModel);

        foreach (var artist in allArtists.Select(x => x.Key))
        {
          artist.IsInPlaylist = true;
        }

        var allAlbums = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).GroupBy(x => x.AlbumViewModel);

        foreach (var album in allAlbums.Select(x => x.Key))
        {
          album.IsInPlaylist = true;
        }
      }

      DownloadInfos(PlayList.ToList());

      var actualDownloadingSongTask = new CancellationTokenSource();
      downloadingSongTasks.Add(actualDownloadingSongTask);

      try
      {
        await DownloadAllLyrics(PlayList.OfType<SongInPlayListViewModel>().ToList(), actualDownloadingSongTask.Token);
      }
      catch (Exception)
      {
      }
    }

    #endregion

    #region ClearPlaylist

    public override Task ClearPlaylist()
    {
      if (PlayList.Count > 0)
      {
        var allArtists = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).GroupBy(x => x.ArtistViewModel);

        foreach (var artist in allArtists.Select(x => x.Key))
        {
          artist.IsInPlaylist = false;
        }

        var allAlbums = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).GroupBy(x => x.AlbumViewModel);

        foreach (var album in allAlbums.Select(x => x.Key))
        {
          album.IsInPlaylist = false;
        }
      }

      return base.ClearPlaylist();
    }

    #endregion

    #region DownloadSongInfos

    protected override Task DownloadInfos(IEnumerable<SoundItemInPlaylistViewModel> soundItemInPlaylistViewModel)
    {
      return Task.Run(async () =>
      {
        modelsDisposables.ForEach(x => x.Dispose());
        modelsDisposables.Clear();

        downloadingSongTasks.Where(x => !x.IsCancellationRequested).ForEach(x => x.Cancel());
        var actualDownloadingSongTask = new CancellationTokenSource();

        downloadingSongTasks.Add(actualDownloadingSongTask);

        ResetDownload();

        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          WasAllItemsProcessed = false;
          CheckedFiles.Clear();
        });

        try
        {
          var items = soundItemInPlaylistViewModel.ToList();
          var notNullItems = items.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null && x.AlbumViewModel != null).ToList();
          var nullItems = items.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel == null || x.AlbumViewModel == null).Select(x => (SoundItemInPlaylistViewModel)x).ToList();

          foreach (var song in notNullItems)
          {
            if (actualDownloadingSongTask.IsCancellationRequested)
            {
              return;
            }

            if (song.Model.Duration <= 0)
              await GetMediaInfo(song.Model);

            MarkViewModelAsChecked(song);
          }

          foreach (var item in nullItems)
          {
            if (actualDownloadingSongTask.IsCancellationRequested)
            {
              return;
            }

            actualDownloadingSongTask.Token.ThrowIfCancellationRequested();

            await DownloadSongInfo(item, actualDownloadingSongTask.Token);
            await GetMediaInfo(item.Model);
          }

          modelsDisposables.Add(storageManager.ObserveOnItemChange<Song>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSongChange).DisposeWith(this));
          modelsDisposables.Add(storageManager.ObserveOnItemChange<Album>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnAlbumChange).DisposeWith(this));
          modelsDisposables.Add(storageManager.ObserveOnItemChange<SoundItem>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSoundItemUpdated).DisposeWith(this));

          VSynchronizationContext.PostOnUIThread(() =>
          {
            WasAllItemsProcessed = true;
          });

          TrySetNewPlaylistName();


        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
        finally
        {
          ResetDownload();
        }
      });
    }

    #endregion

    #region ItemsRemoved

    protected override void ItemsRemoved(EventPattern<SoundItemInPlaylistViewModel> eventPattern)
    {
      if (eventPattern.EventArgs is SongInPlayListViewModel songInPlayListViewModel &&
          songInPlayListViewModel.AlbumViewModel != null &&
          songInPlayListViewModel.ArtistViewModel != null)
      {
        var anyAlbum = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.AlbumViewModel != null)
          .Any(x => x.AlbumViewModel.ModelId == songInPlayListViewModel.AlbumViewModel.ModelId);

        if (!anyAlbum && songInPlayListViewModel.AlbumViewModel != null)
        {
          songInPlayListViewModel.AlbumViewModel.IsInPlaylist = false;
        }

        var anyArtist = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.ArtistViewModel != null)
          .Any(x => x.ArtistViewModel.ModelId == songInPlayListViewModel.ArtistViewModel.ModelId);

        if (!anyArtist && songInPlayListViewModel.ArtistViewModel != null)
        {
          songInPlayListViewModel.ArtistViewModel.IsInPlaylist = false;
        }

        shuffleList.Remove(eventPattern.EventArgs);
      }

    }

    #endregion

    #region ItemsAdded

    private void ItemsAdded(EventPattern<SoundItemInPlaylistViewModel> eventPattern)
    {

    }

    #endregion

    #region CheckCycle

    private void CheckCycle()
    {
      if (playBookInCycle.Count > 0 && playBookInCycle.All(x => x.Value))
      {
        Cycle++;

        foreach (var item in playBookInCycle)
        {
          playBookInCycle[item.Key] = false;
        }
      }
    }

    #endregion

    #region TrySetNewPlaylistName

    private void TrySetNewPlaylistName()
    {
      if (ActualSavedPlaylist != null && string.IsNullOrEmpty(ActualSavedPlaylist.Name))
      {
        ActualSavedPlaylist.Name = GetNewPlaylistName();

        VSynchronizationContext.PostOnUIThread(() =>
        {
          OnSavePlaylist();
        });
      }
    }

    #endregion

    #region GetNewPlaylistName

    private string GetNewPlaylistName()
    {
      var actaulPlaylist = PlayList?.ToList();

      if (actaulPlaylist == null)
      {
        return null;
      }

      var artists = actaulPlaylist.OfType<SongInPlayListViewModel>()
        .Where(x => !string.IsNullOrEmpty(x?.ArtistViewModel?.Name))
        .GroupBy(x => x.ArtistViewModel.Name).ToList();

      string playlistName = "";

      if (artists.Count > 0)
      {
        playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());

        var albums = actaulPlaylist.OfType<SongInPlayListViewModel>()
          .Where(x => !string.IsNullOrEmpty(x?.AlbumViewModel?.Name))
          .GroupBy(x => x.AlbumViewModel.Name).ToList();

        if (albums.Count == 1)
        {
          playlistName += $" - {albums[0].Key}";
        }
      }
      else
      {
        var splits = actaulPlaylist.Where(x => x?.Model?.Source != null)
          .Select(x => x.Model.Source.Split("\\")).ToList();

        if (splits.Count > 0)
        {
          for (int i = 0; i < splits.Max(x => x.Length); i++)
          {
            var source = splits.Where(x => x.Length > i).GroupBy(x => x[i]).ToList();

            var keys = source.Select(x => x.Key).Distinct().ToList();
            var separator = "\\";

            if (keys.Count == 1)
            {
              if (i > 0)
              {
                playlistName += separator;
              }

              playlistName += keys[0];
            }
            else if (i == 0)
            {
              playlistName = null;
            }
          }
        }
        else
        {
          playlistName = null;
        }
      }

      return playlistName;
    }

    #endregion

    #region BeforeClearPlaylist

    protected override void BeforeClearPlaylist()
    {
      base.BeforeClearPlaylist();

      PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).Select(x => x.AlbumViewModel).Distinct()
         .ForEach(x =>
         {
           x.IsPlaying = false;
           x.IsInPlaylist = false;
         });

      PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).Select(x => x.ArtistViewModel).Distinct()
        .ForEach(x =>
        {
          x.IsPlaying = false;
          x.IsInPlaylist = false;
        });


      downloadingSongTasks.Where(x => !x.IsCancellationRequested).ForEach(x => x.Cancel());
    }

    #endregion

    #region GetNewPlaylistModel

    protected override SoundItemFilePlaylist GetNewPlaylistModel(List<PlaylistSoundItem> playlistModels, bool isUserCreated)
    {
      var playlistName = GetNewPlaylistName();

      var entityPlayList = new SoundItemFilePlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = string.IsNullOrEmpty(playlistName) ? "" : playlistName,
        ItemCount = playlistModels?.Count,
        PlaylistItems = playlistModels,
        LastItemElapsedTime = ActualSavedPlaylist.LastItemElapsedTime,
        LastItemIndex = ActualSavedPlaylist.LastItemIndex,
        IsUserCreated = isUserCreated,
        LastPlayed = DateTime.Now
      };

      return entityPlayList;
    }

    #endregion

    #region FilterByActualSearch

    protected override void FilterByActualSearch(string predictate)
    {
      if (!string.IsNullOrEmpty(predictate))
      {
        var items = PlayList.Where(x => IsInFind(x.Name, predictate)).ToList();

        var other = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.AlbumViewModel != null)
          .Where(x => x.ArtistViewModel != null)
          .Where(x => IsInFind(x.AlbumViewModel.Name, predictate) ||
                      IsInFind(x.ArtistViewModel.Name, predictate));

        var notIn = other.Where(x => !items.Contains(x));

        items.AddRange(notIn);

        var generator = new ItemsGenerator<SoundItemInPlaylistViewModel>(items, 15);

        VirtualizedPlayList = new VirtualList<SoundItemInPlaylistViewModel>(generator);

      }
      else
      {
        RequestReloadVirtulizedPlaylist();
      }
    }



    #endregion

    #region OnAlbumUpdated

    private void OnAlbumUpdated(ItemUpdatedEventArgs<AlbumViewModel> itemUpdatedEventArgs)
    {
      if (itemUpdatedEventArgs.Model != null)
      {
        var songsInAlbum = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null)
          .Where(x => x.AlbumViewModel.ModelId == itemUpdatedEventArgs.Model.ModelId);

        foreach (var songInAlbum in songsInAlbum)
        {
          if (songInAlbum.AlbumViewModel != itemUpdatedEventArgs.Model)
            songInAlbum.AlbumViewModel = itemUpdatedEventArgs.Model;
          else
            songInAlbum.UpdateAlbumViewModel(itemUpdatedEventArgs.Model.Model);
        }
      }
    }

    #endregion

    #region SetVlcPlayer

    private async Task SetVlcPlayer()
    {
      MediaPlayer = vLcPlayer;

      await SetMedia(ActualItem.Model);

      if (IsPlaying)
      {
        MediaPlayer.Play();
      }

      isUPnp = false;
    }

    #endregion

    #region OnDeactived

    public override void OnDeactived()
    {
      base.OnDeactived();

      //downloadingLyricsTask?.Cancel();
      //downloadingLyricsTask?.Dispose();
      //downloadingLyricsTask = null;

      //downloadingSongTask?.Cancel();
      //downloadingSongTask?.Dispose();
      //downloadingSongTask = null;
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      downloadingSongTasks.Where(x => !x.IsCancellationRequested).ForEach(x => x.Cancel());
    }

    #endregion

    #region GetMediaInfo

    protected override async Task GetMediaInfo(SoundItem model)
    {
      try
      {
        if (!string.IsNullOrEmpty(model?.Source)
            && !model.Source.Contains("https://")
            && !model.Source.Contains("http://"))
        {
          var mediaAnalysis = await FFProbe.AnalyseAsync(model.Source);
          var vm = PlayList.SingleOrDefault(x => x.Model == model);

          if (vm != null)
            vm.MediaInfo = mediaAnalysis;
        }
      }
      catch (Exception ex)
      {
      }
    }

    #endregion

    #region OnDownloadInfoEvent

    protected override void OnDownloadInfoEvent(SoundItemInPlaylistViewModel itemViewModel)
    {
      Task.Run(async () =>
      {
        ResetDownload();

        var actualDownloadingSongTask = new CancellationTokenSource();
        downloadingSongTasks.Add(actualDownloadingSongTask);

        await DownloadSongInfo(itemViewModel, actualDownloadingSongTask.Token);
      });
    }

    #endregion

    #region SaveData

    protected override async Task SaveData(IEnumerable<SoundItemInPlaylistViewModel> itemViewModels)
    {
      var list = itemViewModels.ToList();

      await storageManager.UpdateEntitiesAsync(list.Select(x => x.Model.FileInfoEntity));

      await storageManager.ResetSongs(list.OfType<SongInPlayListViewModel>().Select(x => x.SongModel));
    }

    #endregion

    #region ResetDownload

    private void ResetDownload()
    {
      originalDownlaodedArtistName = null;
      originalDownlaodedAlbumName = null;
      downloadingAlbum = null;
      downloadingArtist = null;
    }

    #endregion

    #region SortPlaylist

    protected override void SortPlaylist(PlaylistSortOrder playlistSort)
    {
      base.SortPlaylist(playlistSort);

      switch (playlistSort)
      {
        case PlaylistSortOrder.Created:
          break;
        case PlaylistSortOrder.ItemProperties:
          var comp = Comparer<SoundItemInPlaylistViewModel>.Create((x, y) =>
          {
            string albumName1 = null;
            string albumName2 = null;

            string artistName1 = null;
            string artistName2 = null;

            if (x == null && y != null)
              return 1;
            if (y == null && x != null)
              return 0;

            if (x is SongInPlayListViewModel song1)
            {
              albumName1 = song1.AlbumViewModel?.Name;
              artistName1 = song1.ArtistViewModel?.Name;
            }

            if (y is SongInPlayListViewModel song2)
            {
              albumName2 = song2.AlbumViewModel?.Name;
              artistName2 = song2.ArtistViewModel?.Name;
            }

            var name = x.Name.CompareTo(y.Name);
            var album = albumName1?.CompareTo(albumName2) ?? albumName2?.CompareTo(albumName1) * -1 ?? 0;
            var artist = artistName1?.CompareTo(artistName2) ?? artistName2?.CompareTo(artistName1) * -1 ?? 0;

            return artist != 0 ? artist : album != 0 ? album : name;
          });

          PlayList.Sort(comp);
          break;
      }
    }

    #endregion

    #endregion
  }

  public class SoundSliderPopupDetailViewModel : FileItemSliderPopupDetailViewModel<SoundItem>
  {
    public SoundSliderPopupDetailViewModel(SoundItem model) : base(model)
    {
      Image = GetEmptyImage();
    }
  }
}