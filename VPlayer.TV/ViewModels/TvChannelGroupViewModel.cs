using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.IPTV.ViewModels;
using VPlayer.IPTV.ViewModels.Prompts;
using VPlayer.IPTV.Views.Prompts;

namespace VPlayer.IPTV
{
  public class TvChannelGroupViewModel : TreeViewItemViewModel<TvChannelGroup>
  {
    private readonly TVPlayerViewModel tVPlayerViewModel;
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;

    public TvChannelGroupViewModel(
      TvChannelGroup model,
      TVPlayerViewModel tVPlayerViewModel,
      IStorageManager storageManager,
      IWindowManager windowManager) : base(model)
    {
      this.tVPlayerViewModel = tVPlayerViewModel ?? throw new ArgumentNullException(nameof(tVPlayerViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    public override void Initialize()
    {
      base.Initialize();

      SubItems.OnActualItemChanged.Subscribe(x => tVPlayerViewModel.ActualChannel = (TvChannelViewModel)x).DisposeWith(this);
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
          tvChannelDropped = new ActionCommand<object>(OnAddNewSource);
        }

        return tvChannelDropped;
      }
    }

    public void OnAddNewSource(object dropData)
    {
      IDataObject data = dropData as IDataObject;

      if (null == data) return;

      var tvChannel = (TvChannelViewModel)data.GetData(data.GetFormats()[0]);

      if (Model.TVChannels == null)
      {
        Model.TVChannels = new List<TvChannel>();
      }

      if (tvChannel != null && Model.TVChannels.All(x => x.Id != tvChannel.Model.Id))
      {
        Model.TVChannels.Add(tvChannel.Model);

        SubItems.Add(tvChannel.Copy());

        CanExpand = true;
        IsExpanded = true;

        RaisePropertyChanged( nameof(SubItems));
      }
    }

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

    public void OnDelete()
    {
      var question = windowManager.ShowYesNoPrompt($"Do you really want to delete {Name}?", "Delete tv group");

      if (question == System.Windows.MessageBoxResult.Yes)
      {
        var result = storageManager.DeleteEntity(Model);
      }
    }

    #endregion

    #endregion
  }
}