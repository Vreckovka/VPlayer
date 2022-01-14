using LibVLCSharp.Shared.Structures;
using VCore.Standard;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public interface IDescriptedEntity
  {
    string Description { get; }
  }

  public interface IOrdered
  {
    int OrderNumber { get; }
  }


  public abstract class VideoProperty : ViewModel, ISelectable, IDescriptedEntity, IOrdered
  {
    protected VideoProperty() 
    {
    }

    public abstract int OrderNumber { get; }

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