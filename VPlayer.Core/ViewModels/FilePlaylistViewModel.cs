using Prism.Events;
using VCore.WPF.Interfaces.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Library.ViewModels
{
  public abstract class FilePlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel,TModel> : PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>
    where TPlaylistModel : class, IFilePlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IItemInPlaylist<TModel>
    where TModel : class, IEntity
  {
    protected FilePlaylistViewModel(TPlaylistModel model, IEventAggregator eventAggregator, IStorageManager storageManager, IWindowManager windowManager) : base(model, eventAggregator, storageManager, windowManager)
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