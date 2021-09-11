using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Home.ViewModels.Artists;
using VPlayer.Home.Views.Music.Albums;

namespace VPlayer.Home.ViewModels.Albums
{
  public class AlbumDetailViewModel : DetailViewModel<AlbumViewModel, Album, AlbumDetailView>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly IStatusManager statusManager;
    private readonly ILogger logger;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public AlbumDetailViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      AlbumViewModel album,
      IStorageManager storageManager,
      IWindowManager windowManager,
      AudioInfoDownloader audioInfoDownloader,
      StatusManager statusManager,
      ILogger logger
      ) : base(regionProvider, storageManager, album, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion Constructors

    #region Properties

    #region Songs

    private List<SongDetailViewModel> allSong;
    private VirtualList<SongDetailViewModel> songs;

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

    #region TotalDuration

    private TimeSpan totalDuration;

    public TimeSpan TotalDuration
    {
      get { return totalDuration; }
      set
      {
        if (value != totalDuration)
        {
          totalDuration = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsAllSongHasLyricsOff

    private bool isAllSongHasLyricsOff;

    public bool IsAllSongHasLyricsOff
    {
      get { return isAllSongHasLyricsOff; }
      set
      {
        if (value != isAllSongHasLyricsOff)
        {
          isAllSongHasLyricsOff = value;

          ChangeIsAutomaticLyricsFindEnabledForAllSongs(!value);
          RaisePropertyChanged();
        }
      }
    }

    private void SetIsAllSongHasLyricsOff(bool value)
    {
      isAllSongHasLyricsOff = value;
      RaisePropertyChanged(nameof(IsAllSongHasLyricsOff));
    }

    #endregion

    #endregion Properties

    #region GetCovers

    private ActionCommand getCovers;

    public ICommand GetCovers
    {
      get
      {
        if (getCovers == null)
        {
          getCovers = new ActionCommand(OnGetCovers);
        }

        return getCovers;
      }
    }

    protected void OnGetCovers()
    {
      var covers = viewModelsFactory.Create<AlbumCoversViewModel>(ViewModel, RegionName);

      covers.IsActive = true;
    }

    #endregion GetCovers

    #region OnUpdate

    protected override void OnUpdate()
    {
      Task.Run(() =>
      {
        var songs = Songs.ToList();

        var statusMessage = new StatusMessageViewModel(songs.Count)
        {
          Status = StatusType.Processing,
          Message = "Updating album songs from fingerprint"
        };

        statusManager.UpdateMessage(statusMessage);

        if (songs.Count == 0)
        {
          statusMessage.Status = StatusType.Failed;
          statusMessage.Message = "Album has no songs";

          statusManager.UpdateMessage(statusMessage);
          return;
        }

        try
        {
          foreach (var song in songs)
          {
            var audioInfo = audioInfoDownloader.GetAudioInfoByFingerPrint(song.Model.Source);

            if (audioInfo != null)
            {
              song.Model.ItemModel.FileInfo.Name = audioInfo.Title;
            }

            storageManager.UpdateEntityAsync(song.Model);

            statusMessage.ProcessedCount++;

            statusManager.UpdateMessage(statusMessage);
          }

          statusMessage.Status = StatusType.Done;
          statusManager.UpdateMessage(statusMessage);
        }
        catch (Exception ex)
        {
          statusMessage.Status = StatusType.Error;
          statusManager.UpdateMessage(statusMessage);

          logger.Log(ex);
        }
      });
    }

    #endregion

    protected override Task LoadEntity()
    {
      return Task.Run(() =>
      {
        var songsDb = storageManager.GetRepository<Song>()
          .Where(x => x.Album == ViewModel.Model)
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        Application.Current.Dispatcher.Invoke(() =>
        {
          allSong = songsDb.Select(x => viewModelsFactory.Create<SongDetailViewModel>(x)).ToList();
          var generator = new ItemsGenerator<SongDetailViewModel>(allSong, 25);

          Songs = new VirtualList<SongDetailViewModel>(generator);
          TotalDuration = TimeSpan.FromSeconds(allSong.Sum(x => x.Model.Duration));

          SetIsAllSongHasLyricsOff(allSong.All(x => !x.Model.ItemModel.IsAutomaticLyricsFindEnabled));
        });
      });
    }


    private void ChangeIsAutomaticLyricsFindEnabledForAllSongs(bool value)
    {
      using (var context = new AudioDatabaseContext())
      {
        var songs = storageManager.GetRepository<Song>(context)
          .Where(x => x.Album == ViewModel.Model)
          .Include(x => x.ItemModel);

        foreach (var song in songs)
        {
          song.ItemModel.IsAutomaticLyricsFindEnabled = value;
          song.Chartlyrics_Lyric = null;
          song.Chartlyrics_LyricCheckSum = null;
          song.Chartlyrics_LyricId = null;
          song.LRCLyrics = null;

          context.Update(song.ItemModel);
        }

        context.SaveChanges();

        foreach (var item in allSong)
        {
          item.IsAutomaticDownload = !value;

          storageManager.PublishItemChanged(item.Model);
        }
      }
    }
  }
}