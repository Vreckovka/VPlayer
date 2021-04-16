using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Modularity.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;
using VPlayer.IPTV.ViewModels.Prompts;
using VPlayer.IPTV.Views.Prompts;

namespace VPlayer.IPTV
{
  public class TvChannelGroupViewModel : TreeViewItemViewModel<TvChannelGroup>, ITvPlayableItem
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private readonly IVPlayerViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;

    #endregion

    #region Constructors

    public TvChannelGroupViewModel(
      TvChannelGroup model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IVPlayerViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    #endregion

    #region Properties

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

    #region SelectedTvChannel

    private ITvChannel selectedTvChannel;

    public ITvChannel SelectedTvChannel
    {
      get { return selectedTvChannel; }
      set
      {
        if (value != selectedTvChannel)
        {
          if (selectedTvChannel != null)
          {
            selectedTvChannel.IsSelectedToPlay = false;
          }

          selectedTvChannel = value;
          selectedTvChannel.IsSelectedToPlay = true;

          RaisePropertyChanged();
        }
      }
    }


    #endregion

    public IEnumerable<ITvChannel> TvChannelsSources { get; set; }

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

    #region Methods

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

      SubItems.OnActualItemChanged.Where(x => x != null).OfType<TvChannelItemGroupViewModel>().Subscribe(OnSelectionChanged).DisposeWith(this);

      storageManager.SubscribeToItemChange<TvChannelGroupItem>(OnTvChannelChanged).DisposeWith(this);

      TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>();
      SelectedTvChannel = TvChannelsSources.FirstOrDefault();

      SubItems.View.ItemAdded.Subscribe(x =>
      {
        TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>();
      }).DisposeWith(this);

      SubItems.View.ItemRemoved.Subscribe(x =>
      {
        TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>();
      }).DisposeWith(this);
    }

    #endregion

    #region OnSelectionChanged

    private void OnSelectionChanged(TvChannelItemGroupViewModel tvChannelItemGroupViewModel)
    {
      if (tvChannelItemGroupViewModel.IsSelected)
      {
        SelectedTvChannel = tvChannelItemGroupViewModel;
      }
    }

    #endregion

    #region PlayActualTvChannel

    private void PlayActualTvChannel()
    {
      if (SelectedTvChannel != null)
      {
        var eventToPublis = eventAggregator.GetEvent<PlayItemsEvent<TvPlaylistItem, TvItemInPlaylistItemViewModel>>();
        var thisInterfaced = (ITvPlayableItem)this;

        var arguemts = viewModelsFactory.CreateTvItemInPlaylistItemViewModel(new TvPlaylistItem()
        {
          Name = Name
        }, thisInterfaced);


        var data = new PlayItemsEventData<TvItemInPlaylistItemViewModel>(arguemts.GetEnummerable(), EventAction.Play, this)
        {
          StorePlaylist = false,
          SetItemOnly = true
        };

        SelectedTvChannel.IsSelected = true;
        eventToPublis.Publish(data);
      }
    }

    #endregion

    #region OnTvChannelChanged

    private void OnTvChannelChanged(ItemChanged<TvChannelGroupItem> itemChanged)
    {
      switch (itemChanged.Changed)
      {
        case Changed.Added:
          break;
        case Changed.Removed:
          var itemToRemove = SubItems.ViewModels.SingleOrDefault(y => ((TvChannelItemGroupViewModel)y).Model.Id == itemChanged.Item.Id);
          if (itemToRemove != null)
            SubItems.Remove(itemToRemove);
          break;
        case Changed.Updated:
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region OnSelected

    protected override void OnSelected(bool isSelected)
    {
      if (!isSelected)
      {
        if (SelectedTvChannel is TvChannelItemGroupViewModel itemGroupViewModel && itemGroupViewModel.TvChannel is TvStalkerChannelViewModel tvStalker)
        {
          tvStalker.DisposeKeepAlive();
        }
      }
      else
      {
        PlayActualTvChannel();
      }

    }

    #endregion

    #endregion
  }
}