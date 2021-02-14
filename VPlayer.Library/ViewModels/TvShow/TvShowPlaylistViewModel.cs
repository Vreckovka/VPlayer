using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Prism.Events;
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShow;

namespace VPlayer.Library.ViewModels
{
  public class TvShowPlaylistViewModel : PlayableViewModel<TvShowEpisodeInPlaylistViewModel, TvShowPlaylist>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public TvShowPlaylistViewModel(
      TvShowPlaylist model,
      IEventAggregator eventAggregator,
      [NotNull] IStorageManager storage,
      [NotNull] IViewModelsFactory viewModelsFactory) : base(model, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
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

    #region Update

    public override void Update(TvShowPlaylist updateItem)
    {
      this.Model.Update(updateItem);
      RaisePropertyChanges();
    }

    #endregion

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    public override IEnumerable<TvShowEpisodeInPlaylistViewModel> GetItemsToPlay()
    {
      var playlist = storage.GetRepository<TvShowPlaylist>().Include(x => x.PlaylistItems.Select(y => y.TvShowEpisode)).SingleOrDefault(x => x.Id == Model.Id);

      if(playlist != null)
      {
        var playListSong = playlist.PlaylistItems.Select(x => viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(x.TvShowEpisode));

        return playListSong;
      }

      return null;
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayTvShowEventData(viewModels, eventAction, Model);

      eventAggregator.GetEvent<PlayTvShowEvent>().Publish(e);
    }

    #endregion


    public override void PublishAddToPlaylistEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }
  }
}