using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore;
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

    #endregion

    #region Constructors

    public PlaylistViewModel(Playlist model, IEventAggregator eventAggregator, IViewModelsFactory viewModelsFactory, PlaylistsRepository playlistsRepository) : base(model, eventAggregator)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.playlistsRepository = playlistsRepository ?? throw new ArgumentNullException(nameof(playlistsRepository));
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

    #endregion

    public override void Update(Playlist updateItem)
    {
      this.Model.Update(updateItem);
      RaisePropertyChanges();
    }

    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      Random random = new Random();

      Model.Name = "Pregenerovan" + random.Next(0, 1000);
      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(IsUserCreated));
    }

    #endregion

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    public override IEnumerable<SongInPlayList> GetSongsToPlay()
    {
      var songs = playlistsRepository.Context.Playlists.Where(x => x.Id == ModelId).Include(x => x.PlaylistSongs.Select(y => y.Song.Album)).Single();
      return songs.PlaylistSongs.Select(x => viewModelsFactory.Create<SongInPlayList>(x.Song));
    }

    protected override void OnPlay(PlaySongsAction action)
    {
      var data = GetSongsToPlay();

      var e = new PlaySongsEventData()
      {
        PlaySongsAction = action,
        Songs = data,
        IsRepeat = IsRepeating,
        IsShufle = IsShuffle,
        SetPostion = Model.LastSongElapsedTime,
        IdModel = Model.Id
      };

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

  }
}
