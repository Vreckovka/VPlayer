using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels.Albums;
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

    public IEnumerable<Song> AlbumSongs => ViewModel?.Model?.Songs;

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
        var songs = AlbumSongs.ToList();

        var statusMessage = new StatusMessage(songs.Count)
        {
          MessageStatusState = MessageStatusState.Processing,
          Message = "Updating album songs from fingerprint"
        };

        statusManager.UpdateMessage(statusMessage);

        if (songs.Count == 0)
        {
          statusMessage.MessageStatusState = MessageStatusState.Failed;
          statusMessage.FailedMessage = "Album has no songs";

          statusManager.UpdateMessage(statusMessage);
          return;
        }

        try
        {
          foreach (var song in songs)
          {
            var audioInfo = audioInfoDownloader.GetAudioInfoByFingerPrint(song.Source);

            if (audioInfo != null)
            {
              song.Name = audioInfo.Title;
            }

            storageManager.UpdateEntityAsync(song);

            statusMessage.ProcessedCount++;

            statusManager.UpdateMessage(statusMessage);
          }

          statusMessage.MessageStatusState = MessageStatusState.Done;
          statusManager.UpdateMessage(statusMessage);
        }
        catch (Exception ex)
        {
          statusMessage.MessageStatusState = MessageStatusState.Failed;
          statusManager.UpdateMessage(statusMessage);

          logger.Log(ex);
        }
      });
    }

    #endregion

  }
}