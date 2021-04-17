using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Library.ViewModels
{
  public abstract class FilePlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel> : PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel>
    where TPlaylistModel : class, IFilePlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IEntity
  {
    protected FilePlaylistViewModel(TPlaylistModel model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
    }

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

    public override void RaisePropertyChanges()
    {
      base.RaisePropertyChanges();

      RaisePropertyChanged(nameof(IsShuffle));
      RaisePropertyChanged(nameof(IsRepeating));
    }
  }
}