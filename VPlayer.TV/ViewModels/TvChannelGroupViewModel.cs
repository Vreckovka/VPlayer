using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Modularity.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.Events;
using VPlayer.IPTV.ViewModels;
using VPlayer.IPTV.ViewModels.Prompts;
using VPlayer.IPTV.Views.Prompts;

namespace VPlayer.IPTV
{
  public class TvChannelGroupViewModel : TvTreeViewItem<TvChannelGroup>, ITvPlayableItem
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private readonly IVPlayerViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public TvChannelGroupViewModel(
      TvChannelGroup model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IVPlayerViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(model, storageManager, windowManager)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));


    }

    #endregion

    #region Properties

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
            selectedTvChannel.ActualCancellationTokenSource?.Cancel();
            selectedTvChannel.IsSelectedToPlay = false;
          }

          selectedTvChannel = value;
          selectedTvChannel.IsSelectedToPlay = true;

          if (SubItems.View.Any(x => x.IsSelected))
          {
            PlayActualTvChannel();
          }

          RaisePropertyChanged();
        }
      }
    }


    #endregion

    public IEnumerable<ITvChannel> TvChannelsSources { get; set; }

    #endregion

    #region Methods

    #region Initialize

    private bool wasInitilized;
    public override void Initialize()
    {
      SubItems.Clear();

      if (Model.TvChannelGroupItems != null)
      {
        foreach (var tvChannelGroupItem in Model.TvChannelGroupItems)
        {
          TvChannel dbChannel = tvChannelGroupItem.TvChannel;

          if (dbChannel?.TvItem == null)
          {
            dbChannel = storageManager.GetRepository<TvChannel>().Include(x => x.TvItem).Include(x => x.TvSource).Single(x => x.Id == tvChannelGroupItem.IdTvChannel);
            tvChannelGroupItem.TvChannel = dbChannel;
          }


          SubItems.Add(viewModelsFactory.Create<TvChannelItemGroupViewModel>(tvChannelGroupItem));
        }
      }


      CanExpand = SubItems.View.Count > 0;

      SubItems.OnActualItemChanged.Where(x => x != null).OfType<TvChannelItemGroupViewModel>().Subscribe(OnSelectionChanged).DisposeWith(this);

      storageManager.SubscribeToItemChange<TvChannelGroupItem>(OnTvChannelChanged).DisposeWith(this);

      TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>();
      SelectedTvChannel = TvChannelsSources.FirstOrDefault();

      SubItems.View.ItemAdded.Subscribe(x => { TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>(); }).DisposeWith(this);

      SubItems.View.ItemRemoved.Subscribe(x =>
      {
        TvChannelsSources = SubItems.View.OfType<TvChannelItemGroupViewModel>();
        SelectedTvChannel = SubItems.ViewModels.OfType<TvChannelItemGroupViewModel>().FirstOrDefault();
      }).DisposeWith(this);
    }

    #endregion

    #region OnTvChannelDropped

    protected override async void OnTvChannelDropped(object dropData)
    {
      IDataObject data = dropData as IDataObject;

      if (null == data) return;

      var tvChannelViewModel = data.GetData(data.GetFormats()[0]) as TvChannelViewModel;

      if(tvChannelViewModel == null)
      {
        return;
      }

      if (Model.TvChannelGroupItems == null)
      {
        Model.TvChannelGroupItems = new List<TvChannelGroupItem>();
      }

      if (tvChannelViewModel != null && Model.TvChannelGroupItems.All(x => x.Id != tvChannelViewModel.Model.Id))
      {
        var channelGroupItem = new TvChannelGroupItem()
        {
          IdTvChannel = tvChannelViewModel.Model.Id,
          Index = Model.TvChannelGroupItems.Count
        };


        Model.TvChannelGroupItems.Add(channelGroupItem);

        var clones = new List<TvChannelGroupItem>(Model.TvChannelGroupItems.Select(x => x.DeepClone()));

        foreach (var channel in Model.TvChannelGroupItems)
        {
          channel.TvChannel = null;
        }

        CanExpand = true;
        IsExpanded = true;

        RaisePropertyChanged(nameof(SubItems));

        await storageManager.UpdateEntityAsync(Model);

        channelGroupItem.TvChannel = tvChannelViewModel.Model;
        channelGroupItem.TvChannel.TvSource = tvChannelViewModel.Model.TvSource;


        Initialize();
      }
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
      if (SelectedTvChannel == null)
      {
        SelectedTvChannel = SubItems.ViewModels.OfType<TvChannelItemGroupViewModel>().FirstOrDefault();
      }
      
      if (SelectedTvChannel != null)
      {
        var eventToPublis = eventAggregator.GetEvent<PlayChannelEvent>();

        TvChannelsSources = SubItems.ViewModels.OfType<TvChannelItemGroupViewModel>();
        SelectedTvChannel.IsSelected = true;

        eventToPublis.Publish(SelectedTvChannel);
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
          var itemToRemoveVm = (TvChannelItemGroupViewModel)SubItems.ViewModels.SingleOrDefault(y => ((TvChannelItemGroupViewModel)y).Model.Id == itemChanged.Item.Id);
          if (itemToRemoveVm != null)
          {
            SubItems.Remove(itemToRemoveVm);
            Model.TvChannelGroupItems.Remove(itemToRemoveVm.Model);
          }

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

    #region OnDelete

    protected override void OnDelete()
    {
      var question = windowManager.ShowYesNoPrompt($"Do you really want to delete {Name}?", "Delete tv group");

      if (question == System.Windows.MessageBoxResult.Yes)
      {
        storageManager.DeleteTvChannelGroup(Model);
      }
    }

    #endregion

    #endregion
  }
}