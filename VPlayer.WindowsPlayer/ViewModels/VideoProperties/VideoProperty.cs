using LibVLCSharp.Shared.Structures;
using VCore.Standard;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public abstract class VideoProperty : ViewModel<TrackDescription>
  {
    protected VideoProperty(TrackDescription model) : base(model)
    {
    }

    #region Description

    private string description;

    public string Description
    {
      get { return description; }
      set
      {
        if (value != description)
        {
          description = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsSelected

    private bool isSelected;

    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}