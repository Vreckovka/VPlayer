using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;

namespace VPlayer.Home.ViewModels
{
  public abstract class PlaylistsViewModel<TView, TViewModel, TPlaylistModel, TPlaylistItemModel, TItemModel> : PlayableItemsViewModel<TView, TViewModel, TPlaylistModel>
    where TView : class, IView
    where TViewModel : class, INamedEntityViewModel<TPlaylistModel>, IBusy, IPinned
    where TPlaylistModel : class, INamedEntity, IFilePlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IItemInPlaylist<TItemModel>
  {
    private int initTake = 23;
    public PlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
      LibraryCollection<TViewModel, TPlaylistModel> libraryCollection,
      IEventAggregator eventAggregator) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      LoadingStatus = new LoadingStatus()
      {
        ShowProcessCount = false
      };

      LibraryCollection.MaxTake = initTake;
    }

    protected IEnumerable<TViewModel> AllItems { get; set; }

    protected override bool SubscribeToPinned => true;

    #region PrivateItems

    private IEnumerable<TViewModel> privateItems = new List<TViewModel>();

    public IEnumerable<TViewModel> PrivateItems
    {
      get { return privateItems; }
      set
      {
        if (value != privateItems)
        {
          privateItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    protected virtual IQueryable<TPlaylistItemModel> GetActualItemQuery
    {
      get
      {
        return storageManager.GetTempRepository<TPlaylistItemModel>().Include(x => x.ReferencedItem);
      }
    }

    #region AllUserCreatedItems

    private IEnumerable<TViewModel> allUserCreatedItems;

    public IEnumerable<TViewModel> AllUserCreatedItems
    {
      get { return allUserCreatedItems; }
      set
      {
        if (value != allUserCreatedItems)
        {
          allUserCreatedItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AllGeneratedItems

    private IEnumerable<TViewModel> allGeneratedItems;

    public IEnumerable<TViewModel> AllGeneratedItems
    {
      get { return allGeneratedItems; }
      set
      {
        if (value != allGeneratedItems)
        {
          allGeneratedItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShowPrivate

    private bool isShowPrivate;

    public bool IsShowPrivate
    {
      get { return isShowPrivate; }
      set
      {
        if (value != isShowPrivate)
        {
          isShowPrivate = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public override IQueryable<TPlaylistModel> LoadQuery => base.LoadQuery.OrderByDescending(x => x.LastPlayed).Where(x => !x.IsPrivate);

    public ObservableCollection<TViewModel> ViewCollection
    {
      get
      {
        return LibraryCollection.FilteredItemsCollection;
      }
    }

    #region Commands

    #region ShowPrivate

    private ActionCommand showPrivate;

    public ICommand ShowPrivate
    {
      get
      {
        if (showPrivate == null)
        {
          showPrivate = new ActionCommand(OnShowPrivate);
        }

        return showPrivate;
      }
    }

    protected virtual void OnShowPrivate()
    {
      IsShowPrivate = !IsShowPrivate;
    }


    #endregion

    #region LoadMoreItems

    private ActionCommand loadMoreItems;

    public ICommand LoadMoreItems
    {
      get
      {
        if (loadMoreItems == null)
        {
          loadMoreItems = new ActionCommand(OnLoadMoreItems);
        }

        return loadMoreItems;
      }
    }

    protected virtual void OnLoadMoreItems()
    {
      LoadPage();
    }


    #endregion

    #endregion

    #region CanLoadMoreItems

    private bool canLoadMoreItems;

    public bool CanLoadMoreItems
    {
      get { return canLoadMoreItems; }
      set
      {
        if (value != canLoadMoreItems)
        {
          canLoadMoreItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    private int actualSkip = 0;
    private int take = 10;

    protected override void OnDataLoaded()
    {
      AllItems = LibraryCollection.Items.ToList();
      AllUserCreatedItems = LibraryCollection.Items.Where(x => x.Model.IsUserCreated).ToList();
      AllGeneratedItems = LibraryCollection.Items.Where(x => !x.Model.IsUserCreated).ToList();

      var userCreated = AllUserCreatedItems;
      var notSavedPlaylists = AllGeneratedItems.Take(initTake);

      actualSkip += initTake;

      var privateItemsL = storageManager.GetTempRepository<TPlaylistModel>().OrderByDescending(x => x.LastPlayed)
        .Where(x => x.IsPrivate)
        .Select(x => viewModelsFactory.Create<TViewModel>(x))
        .ToList();

      VSynchronizationContext.InvokeOnDispatcher(async () =>
      {
        PrivateItems = privateItemsL;
      });

      GetActualItems();

      VSynchronizationContext.PostOnUIThread(async () =>
      {
        await LoadPinnedItems();

        LibraryCollection.FilteredItemsCollection = new ObservableCollection<TViewModel>(userCreated.Concat(notSavedPlaylists));

        RaisePropertyChanged(nameof(ViewCollection));

        CanLoadMoreItems = actualSkip < AllItems.Count();

        LibraryCollection.Items.ItemUpdated
          .Where(x => x.EventArgs.PropertyName == nameof(IBusy.IsBusy))
          .ObserveOnDispatcher()
          .Subscribe(x =>
          {
            LoadingStatus.IsLoading = ((IBusy)x.Sender).IsBusy;
          }).DisposeWith(this);
      });
    }

    protected void GetActualItems()
    {
      var allItemsWithActualItem = AllItems.Concat(PrivateItems).Where(x => x.Model.ActualItemId != null).ToList();
      var allIds = allItemsWithActualItem.Select(x => x.Model.ActualItemId.Value);

      var allItems = GetActualItemQuery.Where(x => allIds.Contains(x.Id)).ToList();

      foreach (var actualItem in allItems)
      {
        var playList = allItemsWithActualItem.SingleOrDefault(x => x.Model.ActualItemId == actualItem.Id);

        if (playList != null)
          playList.Model.ActualItem = actualItem;
      }
    }

    #region LoadPinnedItems

    private async Task LoadPinnedItems()
    {
      PinnedItems.Clear();

      var items = await Task.Run(() =>
      {
        return storageManager.GetTempRepository<PinnedItem>().OrderBy(x => x.OrderNumber).ToList();
      });

      var typedPinnedItems = GetPinnedTypedItems(items);

      var vms = typedPinnedItems.Select(x => viewModelsFactory.Create<PinnedItemViewModel>(x)).ToList();

      PinnedItems.AddRange(vms);

      if (PinnedItems.Count(x => x.OrderNumber == 0) > 1)
      {
        for (int i = 0; i < PinnedItems.Count; i++)
        {
          PinnedItems[i].OrderNumber = i;
          await storageManager.UpdateEntityAsync(PinnedItems[i].Model);
        }
      }

      for (int i = 0; i < PinnedItems.Count; i++)
      {
        PinnedItems[i].pinnedItemsCollection = PinnedItems;
      }


      foreach (var pinnedItemViewModel in vms.Where(x => x.Model.PinnedType == PinnedType.VideoPlaylist || x.Model.PinnedType == PinnedType.SoundPlaylist))
      {
        var item = ViewModels.SingleOrDefault(x => x.ModelId == int.Parse(pinnedItemViewModel.Model.Description));

        if (item != null)
        {
          pinnedItemViewModel.ItemObject = item;

          item.PinnedItem = pinnedItemViewModel.Model;
          item.IsPinned = true;
        }
      }
    }

    #endregion

    #region SetupNewPinnedItem

    protected override void SetupNewPinnedItem(PinnedItem pinnedItem)
    {
      var vm = viewModelsFactory.Create<PinnedItemViewModel>(pinnedItem);
      var items = GetPinnedTypedItems(new List<PinnedItem>() { pinnedItem });
      var validItem = items.FirstOrDefault();

      if (validItem != null)
      {
        if (validItem.PinnedType == PinnedType.SoundPlaylist || validItem.PinnedType == PinnedType.VideoPlaylist)
        {
          var item = ViewModels.SingleOrDefault(x => x.ModelId == int.Parse(vm.Model.Description));

          if (item != null)
          {
            vm.ItemObject = item;

            item.PinnedItem = vm.Model;
            item.IsPinned = true;
          }
        }

        vm.OrderNumber = PinnedItems.Count;
        vm.pinnedItemsCollection = PinnedItems;
        PinnedItems.Add(vm);
      }
    }

    #endregion

    protected abstract List<PinnedItem> GetPinnedTypedItems(List<PinnedItem> pinnedItems);

    private async void LoadPage()
    {
      var nextPage = AllGeneratedItems.Skip(actualSkip).Take(take);

      await LibraryCollection.AddRange(nextPage);

      actualSkip += take;

      CanLoadMoreItems = actualSkip < AllItems.Count();
      RaisePropertyChanged(nameof(View));
    }

    protected override void OnUpdateItemChange(TPlaylistModel model)
    {
      base.OnUpdateItemChange(model);

      var vm = LibraryCollection.Items?.FirstOrDefault(x => x.ModelId == model.Id);

      if (vm == null)
      {
        vm = PrivateItems.SingleOrDefault(x => x.ModelId == model.Id);

        if (vm != null)
        {
          var actualItem = GetActualItemQuery.SingleOrDefault(x => x.Id == model.ActualItemId);

          if (actualItem != null)
            model.ActualItem = actualItem;

          vm.Update(model);

          var newItemUpdatedArgs = new ItemUpdatedEventArgs<TViewModel>()
          {
            Model = vm
          };

          eventAggregator.GetEvent<ItemUpdatedEvent<TViewModel>>().Publish(newItemUpdatedArgs);
        }
      }

      if (vm != null)
      {
        var actualItem = GetActualItemQuery.SingleOrDefault(x => x.Id == model.ActualItemId);

        if (actualItem != null)
        {
          var modelCopy = vm.Model.DeepClone();
          modelCopy.ActualItem = actualItem;

          VSynchronizationContext.PostOnUIThread(() =>
          {
            vm.Update(modelCopy);
          });
        }
      }
    }
  }
}