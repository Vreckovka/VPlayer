using System;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public abstract class UPnPViewModel<T> : ViewModel<T>, ISelectable where T : global::UPnP.Device.UPnPDevice
  {
    protected readonly IStorageManager storageManager;

    public UPnPViewModel(T model, IStorageManager storageManager) : base(model)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }


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

          if(isSelected)
          {
            OnSelected();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

    #region Save

    protected ActionCommand save;

    public ICommand Save
    {
      get
      {
        if (save == null)
        {
          save = new ActionCommand(OnSave, () => !IsStored);
        }

        return save;
      }
    }

    private void OnSave()
    {
      StoreData();
    }

    #endregion

    #endregion

    #region IsStored

    private bool isStored;

    public bool IsStored
    {
      get { return isStored; }
      set
      {
        if (value != isStored)
        {
          isStored = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion



    public virtual void OnSelected()
    {

    }

    public abstract void StoreData();
  }
}