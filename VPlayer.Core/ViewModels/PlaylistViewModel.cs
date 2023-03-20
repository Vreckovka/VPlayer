using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Library.ViewModels
{
  public interface IPlaylistViewModel
  {

  }

  public abstract class PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel> :
    PlayableViewModel<TPlaylistItemViewModel, TPlaylistModel>, IPlaylistViewModel
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IEntity
  {
    protected readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;


    protected PlaylistViewModel(
      TPlaylistModel model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IWindowManager windowManager) : base(model, eventAggregator)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    #region Properties

    #region IsActive

    private bool isActive;

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string Header
    {
      get { return ToString(); }
    }


    public bool IsUserCreated => Model.IsUserCreated;

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

    #region TotalPlayedTime

    public TimeSpan TotalPlayedTime
    {
      get { return Model.TotalPlayedTime; }
      set
      {
        if (value != Model.TotalPlayedTime)
        {
          Model.TotalPlayedTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public int? ItemsCount => Model.ItemCount;
    public long? HashCode => Model.HashCode;



    #region DisplayName

    public string DisplayName
    {
      get
      {
        if (Directory.Exists(Name))
          return new DirectoryInfo(Name).Name;

        if (File.Exists(Name))
          return new System.IO.FileInfo(Name).Name;

        return Name;
      }
    }

    #endregion





    #endregion

    #region Commands

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
      try
      {
        IsBusy = true;
        bool delete = true;

        if (Model.IsUserCreated)
        {
          var result = windowManager.ShowDeletePrompt(Model.Name);

          if (result != VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
          {
            delete = false;
          }
        }

        if (delete)
          storageManager.DeletePlaylist<TPlaylistModel, TPlaylistItemModel>(Model);
      }
      finally
      {
        IsBusy = false;
      }
    }

    #endregion

    #region PinItem

    private ActionCommand pinItem;

    public ICommand PinItem
    {
      get
      {
        if (pinItem == null)
        {
          pinItem = new ActionCommand(OnPinItem);
        }

        return pinItem;
      }
    }

    public async void OnPinItem()
    {
      var foundItem = storageManager.GetTempRepository<PinnedItem>().SingleOrDefault(x => x.Description == Model.Id.ToString() &&
                                                                                          x.PinnedType == GetPinnedType(Model));

      if (foundItem == null)
      {
        var newPinnedItem = new PinnedItem();
        newPinnedItem.Description = Model.Id.ToString();
        newPinnedItem.PinnedType = GetPinnedType(Model);

        var item = await Task.Run(() => storageManager.AddPinnedItem(newPinnedItem));

        PinnedItem = item;
      }
    }

    #endregion

    #endregion

    #region Methods


    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(LastPlayed));
      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(IsUserCreated));
      RaisePropertyChanged(nameof(ItemsCount));
      RaisePropertyChanged(nameof(HashCode));

      RaisePropertyChanged(nameof(TotalPlayedTime));
      RaisePropertyChanged(nameof(Model));
    }

    #endregion

    #region Update

    public override void Update(TPlaylistModel updateItem)
    {
      this.Model.Update(updateItem);

      RaisePropertyChanges();
    }

    #endregion


    protected abstract PinnedType GetPinnedType(TPlaylistModel model);

    #endregion


  }
}