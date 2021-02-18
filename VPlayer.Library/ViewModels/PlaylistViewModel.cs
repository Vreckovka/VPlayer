using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Repositories;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
{
  public class PlaylistViewModel : PlayableViewModel<SongInPlayList,SongsPlaylist>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly PlaylistsRepository playlistsRepository;
    [NotNull] private readonly SongPlaylistsViewModel songPlaylistsViewModel;
    private readonly IStorageManager storageManager;
    private readonly ILogger logger;
    private readonly IRegionProvider regionProvider;

    #endregion

    #region Constructors

    public PlaylistViewModel(
      SongsPlaylist model,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      PlaylistsRepository playlistsRepository,
      SongsRepository songsRepository,
      [NotNull] SongPlaylistsViewModel songPlaylistsViewModel,
        IStorageManager storageManager,
      ILogger logger) : base(model, eventAggregator)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.playlistsRepository = playlistsRepository ?? throw new ArgumentNullException(nameof(playlistsRepository));
      this.songPlaylistsViewModel = songPlaylistsViewModel ?? throw new ArgumentNullException(nameof(songPlaylistsViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Delete

    private ActionCommand delete;

    public ICommand Delete
    {
      get
      {
        if (delete == null)
        {
          delete = new ActionCommand(OnDelete);
        }

        return delete;
      }
    }

    public void OnDelete()
    {
      storageManager.DeletePlaylist(Model);
    }

    #endregion 

    #region Properties

    public bool IsUserCreated => Model.IsUserCreated;

    #region IsRepeating

    public bool IsRepeating
    {
      get
      {
        return Model.IsReapting;
      }
      set
      {
        if (value != Model.IsReapting)
        {
          Model.IsReapting = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShuffle

    public bool IsShuffle
    {
      get
      {
        return Model.IsShuffle;
      }
      set
      {
        if (value != Model.IsShuffle)
        {
          Model.IsShuffle = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LastPlayed

    public DateTime LastPlayed
    {
      get { return Model.LastPlayed; }
      set
      {
        if (value != Model.LastPlayed)
        {
          Model.LastPlayed = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public int? SongCount => Model.ItemCount;
    public long? SongsInPlaylitsHashCode => Model.HashCode;


    #endregion

    public override void Update(SongsPlaylist updateItem)
    {
      this.Model.Update(updateItem);
      RaisePropertyChanges();
    }

    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(LastPlayed));
      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(IsUserCreated));
      RaisePropertyChanged(nameof(SongCount));
      RaisePropertyChanged(nameof(SongsInPlaylitsHashCode));
      RaisePropertyChanged(nameof(IsShuffle));
      RaisePropertyChanged(nameof(IsRepeating));
    }

    #endregion

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    public override IEnumerable<SongInPlayList> GetItemsToPlay()
    {
      var playlist = playlistsRepository.Entities.Where(x => x.Id == ModelId)
        .Include(x => x.PlaylistItems).ThenInclude(x => x.Song).ThenInclude(x => x.Album)
        .SingleOrDefault();

      if (playlist != null)
      {
        var validSongs = playlist.PlaylistItems.Where(x => x.Song.Album != null).ToList();

        if (validSongs.Count != playlist.PlaylistItems.Count)
        {
          logger.Log(MessageType.Error, $"SONGS WITH NULL ALBUM! {ModelId} {Name}");
        }

        return validSongs.Select(x => viewModelsFactory.Create<SongInPlayList>(x.Song));
      }

      return new List<SongInPlayList>(); ;
    }

    public override void PublishPlayEvent(IEnumerable<SongInPlayList> viewModels, EventAction eventAction )
    {
      var e = new PlaySongsEventData(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SongInPlayList> viewModels)
    {
      var e = new PlaySongsEventData(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    #region OnPlay

    protected override void OnPlay(EventAction action)
    {
      songPlaylistsViewModel.IsBusy = true;

      Task.Run(() =>
      {
        var data = GetItemsToPlay().ToList();

        var e = new PlaySongsEventData(data, action, IsShuffle, IsRepeating, Model.LastItemElapsedTime, Model);

        try
        {
          eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
        }
        catch (Exception ex)
        {

          throw;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
          songPlaylistsViewModel.IsBusy = false;
        });
      });
    }

    #endregion


  }
}