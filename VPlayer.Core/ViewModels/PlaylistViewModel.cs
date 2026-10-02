using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.WPF;
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

  public abstract class PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> :
    PlayableViewModel<TPlaylistItemViewModel, TPlaylistModel>, IPlaylistViewModel
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>, IPlaylist
    where TPlaylistItemModel : class, IItemInPlaylist<TModel>
    where TModel : class, IEntity
  {
    protected readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;


    protected PlaylistViewModel(
      TPlaylistModel model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IWindowManager windowManager) : base(model, eventAggregator, storageManager)
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

    private string displayName;
    public string DisplayName
    {
      get
      {
        return displayName;
      }
      set
      {
        if (value != displayName)
        {
          displayName = value;
          RaisePropertyChanged();
        }
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

    #region ChangePlaylistFavorite

    private ActionCommand<PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>> changePlaylistFavorite;

    public ICommand ChangePlaylistFavorite
    {
      get
      {
        if (changePlaylistFavorite == null)
        {
          changePlaylistFavorite = new ActionCommand<PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>>(OnChangePlaylistFavorite);
        }

        return changePlaylistFavorite;
      }
    }

    public void OnChangePlaylistFavorite(PlaylistViewModel<TPlaylistItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> playlist)
    {
      playlist.Model.IsUserCreated = !playlist.Model.IsUserCreated;

      storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel, TModel>(playlist.Model, out var _);
    }

    #endregion

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      VSynchronizationContext.PostOnUIThread(() =>
      {
        DisplayName = Name;
        GetDisplayName();
      });
    }


    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      GetDisplayName();
      RaisePropertyChanged(nameof(LastPlayed));
      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(IsUserCreated));
      RaisePropertyChanged(nameof(ItemsCount));
      RaisePropertyChanged(nameof(HashCode));

      RaisePropertyChanged(nameof(TotalPlayedTime));
      RaisePropertyChanged(nameof(Model));
    }

    #endregion

    public void GetDisplayName()
    {
      var name = Name;
      if (string.IsNullOrEmpty(name))
      {
        DisplayName = $"GENERATED: {Model.Created?.ToString("dddd,dd MMMM yyyy HH:mm")}";
        return;
      }

      // Titles (including relative text with slashes) stay verbatim. Absolute
      // paths have a stable leaf name even when their drive/share is offline.
      if (Path.IsPathFullyQualified(name))
      {
        var leaf = Path.GetFileName(name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        DisplayName = string.IsNullOrEmpty(leaf) ? name : leaf;
      }
      else
        DisplayName = name;
    }

    public override void RefreshModel(TPlaylistModel model)
    {
      base.RefreshModel(model);
      GetDisplayName();
    }

    #region Update

    public override void Update(TPlaylistModel updateItem)
    {
      this.Model.Update(updateItem);

      RaisePropertyChanges();
    }

    #endregion




    #endregion


  }
}