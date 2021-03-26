using LibVLCSharp.Shared.Structures;
using VCore.Standard;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public abstract class VideoProperty : ViewModel, ISelectable
  {
    protected VideoProperty() 
    {
    }

    #region Description

    public abstract string Description { get;  }

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