using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Home.ViewModels.Artists
{
  public class SongDetailViewModel : DetailItemViewModel<Song>
  {
    public SongDetailViewModel(Song model) : base(model)
    {
    }



    #region IsAutomaticDownload

    public bool IsAutomaticDownload
    {
      get { return !Model.ItemModel.IsAutomaticLyricsFindEnabled; }
      set
      {
        if (value != !Model.ItemModel.IsAutomaticLyricsFindEnabled)
        {
          Model.ItemModel.IsAutomaticLyricsFindEnabled = !value;

          RaisePropertyChanged();
        }
      }
    }

    #endregion


    public void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(IsAutomaticDownload));
      RaisePropertyChanged(nameof(Model));
    }

  }
}