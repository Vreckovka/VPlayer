using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
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
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;

    #region Constructors

    public TvChannelGroupViewModel(
      TvChannelGroup model,
      TVPlayerViewModel tVPlayerViewModel,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(model)
    {
      this.tVPlayerViewModel = tVPlayerViewModel ?? throw new ArgumentNullException(nameof(tVPlayerViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    #endregion

    #region Name

    public string Name
    {
      get { return Model.Name; }
      set
      {
        if (value != Model.Name)
        {
          Model.Name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    public async void OnAddNewSource(object dropData)
    {
      IDataObject data = dropData as IDataObject;

      if (null == data) return;

      var tvChannelViewModel = (TvChannelViewModel)data.GetData(data.GetFormats()[0]);

      if (Model.TvChannels == null)
      {
        Model.TvChannels = new List<TvChannelGroupItem>();
      }

      if (tvChannelViewModel != null && Model.TvChannels.All(x => x.Id != tvChannelViewModel.Model.Id))
      {
        var channelGroupItem = new TvChannelGroupItem()
        {
          IdTvChannel = tvChannelViewModel.Model.Id
        };


        Model.TvChannels.Add(channelGroupItem);

        CanExpand = true;
        IsExpanded = true;

        RaisePropertyChanged(nameof(SubItems));

        await storageManager.UpdateEntityAsync(Model);

        channelGroupItem.TvChannel = tvChannelViewModel.Model;
        channelGroupItem.TvChannel.TvSource = tvChannelViewModel.Model.TvSource;

        SubItems.Add(viewModelsFactory.Create<TvChannelItemGroupViewModel>(channelGroupItem));
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

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      if (Model.TvChannels != null)
        foreach (var tvChannel in Model.TvChannels)
        {
          SubItems.Add(viewModelsFactory.Create<TvChannelItemGroupViewModel>(tvChannel));
        }

      CanExpand = SubItems.View.Count > 0;

      SubItems.OnActualItemChanged.Where(x => x != null).Subscribe(x =>
      {
        if (x.IsSelected)
        {
          tVPlayerViewModel.ActualChannel = ((TvChannelItemGroupViewModel)x).TvChannel;
        }
      }).DisposeWith(this);

      storageManager.SubscribeToItemChange<TvChannelGroupItem>(x =>
      {
        switch (x.Changed)
        {
          case Changed.Added:
           break;
          case Changed.Removed:
            var itemToRemove = SubItems.ViewModels.SingleOrDefault(y => ((TvChannelItemGroupViewModel)y).Model.Id == x.Item.Id);
            if (itemToRemove != null)
              SubItems.Remove(itemToRemove);
            break;
          case Changed.Updated:
          default:
            throw new ArgumentOutOfRangeException();
        }
      });

     

    }

    #endregion
  }
}