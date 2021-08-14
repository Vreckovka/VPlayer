using System;
using System.Windows.Input;
using VCore;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.IPTV
{
  public abstract class TvTreeViewItem<TModel> : TreeViewItemViewModel<TModel> where TModel : class, IEntity, INamedEntity
  {
    protected readonly IStorageManager storageManager;
    protected readonly IWindowManager windowManager;

    public TvTreeViewItem(TModel model, IStorageManager storageManager, IWindowManager windowManager) : base(model)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      Name = model.Name;
    }

  
    #region Commands

    #region TvChannelDropped

    private ActionCommand<object> tvChannelDropped;

    public ICommand TvChannelDropped
    {
      get
      {
        if (tvChannelDropped == null)
        {
          tvChannelDropped = new ActionCommand<object>(OnTvChannelDropped);
        }

        return tvChannelDropped;
      }
    }

    protected abstract void OnTvChannelDropped(object dropData);

    #endregion

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

    protected virtual void OnDelete()
    {
      var question = windowManager.ShowDeletePrompt(Name, header: "Delete tv group");

      if (question == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        var result = storageManager.DeleteEntity(Model);
      }
    }

    #endregion

    #endregion

   
  }
}