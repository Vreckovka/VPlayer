using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Repositories;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.Views;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels
{
  public class PlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, PlaylistViewModel, Playlist>
  {
    public PlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
      LibraryCollection<PlaylistViewModel, Playlist> libraryCollection) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Playlists";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;
  }

  public enum PlaylistCreation
  {
    UserCreated,
    Automatic
  }

  public class PlaylistViewModel : PlayableViewModel<Playlist>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly PlaylistsRepository playlistsRepository;
    private readonly IStorageManager storageManager;
    private readonly ILogger logger;
    private readonly IRegionProvider regionProvider;

    #endregion

    #region Constructors

    public PlaylistViewModel(
      Playlist model, 
      IEventAggregator eventAggregator, 
      IViewModelsFactory viewModelsFactory, 
      PlaylistsRepository playlistsRepository,
      SongsRepository songsRepository,
      IStorageManager storageManager,
      ILogger logger) : base(model, eventAggregator)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.playlistsRepository = playlistsRepository ?? throw new ArgumentNullException(nameof(playlistsRepository));
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

    public int? SongCount => Model.SongCount;
    public long? SongsInPlaylitsHashCode => Model.SongsInPlaylitsHashCode;

    #endregion

    public override void Update(Playlist updateItem)
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

    public override IEnumerable<SongInPlayList> GetSongsToPlay()
    {
      var playlist = playlistsRepository.Context.Playlists.Where(x => x.Id == ModelId).Include(x => x.PlaylistSongs.Select(y => y.Song.Album)).SingleOrDefault();

      if (playlist != null)
      {
        var validSongs = playlist.PlaylistSongs.Where(x => x.Song.Album != null).ToList();

        if (validSongs.Count != playlist.PlaylistSongs.Count)
        {
          logger.Log(MessageType.Error, $"SONGS WITH NULL ALBUM! {ModelId} {Name}");
        }

        return validSongs.Select(x => viewModelsFactory.Create<SongInPlayList>(x.Song));
      }

      return new List<SongInPlayList>(); ;
    }

    protected override void OnPlay(PlaySongsAction action)
    {
      var data = GetSongsToPlay();

      var e = new PlaySongsEventData(data, action, IsShuffle, IsRepeating, Model.LastSongElapsedTime, Model);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }



  }
}
