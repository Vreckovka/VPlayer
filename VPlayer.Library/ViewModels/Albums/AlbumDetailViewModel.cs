using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
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
      IStatusManager statusManager,
      ILogger logger
      ) : base(regionProvider, storageManager, statusManager,album, windowManager)
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
    private VirtualList<SongDetailViewModel> songsVirtualList;

    public VirtualList<SongDetailViewModel> SongsView
    {
      get { return songsVirtualList; }
      set
      {
        if (value != songsVirtualList)
        {
          songsVirtualList = value;
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
        var statusMessage = new StatusMessageViewModel(allSong.Count)
        {
          Status = StatusType.Processing,
          Message = "Updating album songs from fingerprint"
        };

        statusManager.UpdateMessage(statusMessage);

        if (allSong.Count == 0)
        {
          statusMessage.Status = StatusType.Failed;
          statusMessage.Message = "Album has no songs";

          statusManager.UpdateMessage(statusMessage);
          return;
        }

        try
        {
          foreach (var song in allSong)
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

    #region LoadEntity

    protected override Task LoadEntity()
    {
      return Task.Run(() =>
      {
        if (ViewModel.Model.Songs.Count(x => x.ItemModel != null) == 0)
        {
          var songsDb = storageManager.GetTempRepository<Song>()
            .Where(x => x.Album == ViewModel.Model)
            .Include(x => x.ItemModel)
            .ThenInclude(x => x.FileInfo)
            .ToList();

          if (songsDb.Count != 0)
          {
            allSong = songsDb.Select(x => viewModelsFactory.Create<SongDetailViewModel>(x)).ToList();
          }
        }
        else
        {
          allSong = ViewModel.Model.Songs.Select(x => viewModelsFactory.Create<SongDetailViewModel>(x.Copy())).ToList();
        }

        if (allSong != null)
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            var generator = new ItemsGenerator<SongDetailViewModel>(allSong, 25);
            SongsView = new VirtualList<SongDetailViewModel>(generator);

            TotalDuration = TimeSpan.FromSeconds(allSong.Sum(x => x.Model.Duration));

            SetIsAllSongHasLyricsOff(allSong.All(x => !x.Model.ItemModel.IsAutomaticLyricsFindEnabled));
          });
        }
      });
    }


    #endregion

    #region ChangeIsAutomaticLyricsFindEnabledForAllSongs

    private void ChangeIsAutomaticLyricsFindEnabledForAllSongs(bool value)
    {
      using (var context = new AudioDatabaseContext())
      {
        var soundItems = storageManager.GetTempRepository<SoundItem>(context)
          .Where(x => allSong.Select(y => y.Model.ItemModel.Id).Contains(x.Id));

        foreach (var song in soundItems)
        {
          song.IsAutomaticLyricsFindEnabled = value;

          context.Update(song);
        }

        context.SaveChanges();

     
        foreach (var item in allSong)
        {
          item.IsAutomaticDownload = !value;

          storageManager.PublishItemChanged(item.Model);
          storageManager.PublishItemChanged(item.Model.ItemModel);
        }
      }
    }

    #endregion
  }
}