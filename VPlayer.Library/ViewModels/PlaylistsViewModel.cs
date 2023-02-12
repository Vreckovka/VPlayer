using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;

namespace VPlayer.Home.ViewModels
{
  public abstract class PlaylistsViewModel<TView, TViewModel, TPlaylistModel, TPlaylistItemModel, TItemModel> : PlayableItemsViewModel<TView, TViewModel, TPlaylistModel>
    where TView : class, IView
    where TViewModel : class, INamedEntityViewModel<TPlaylistModel>, IBusy
    where TPlaylistModel : class, INamedEntity, IFilePlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IItemInPlaylist<TItemModel>
  {
    public PlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
      LibraryCollection<TViewModel, TPlaylistModel> libraryCollection,
      IEventAggregator eventAggregator) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      LoadingStatus = new LoadingStatus()
      {
        ShowProcessCount = false
      };
    }

    protected IEnumerable<TViewModel> AllItems { get; set; }



    #region PrivateItems

    private IEnumerable<TViewModel> privateItems;

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
        return storageManager.GetRepository<TPlaylistItemModel>().Include(x => x.ReferencedItem);
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

      GetActualItems();

      var initTake = 23;
      var userCreated = AllUserCreatedItems;
      var notSavedPlaylists = AllGeneratedItems.Take(initTake);

      actualSkip += initTake;

      var privateItemsL = storageManager.GetRepository<TPlaylistModel>().OrderByDescending(x => x.LastPlayed)
        .Where(x => x.IsPrivate)
        .Select(x => viewModelsFactory.Create<TViewModel>(x))
        .ToList();


      Application.Current.Dispatcher.Invoke(() =>
      {
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

        PrivateItems = privateItemsL;
      });
    }

    protected void GetActualItems()
    {
      var allItemsWithActualItem = AllItems.Where(x => x.Model.ActualItemId != null).ToList();
      var allIds = allItemsWithActualItem.Select(x => x.Model.ActualItemId.Value);

      var allItems = GetActualItemQuery.Where(x => allIds.Contains(x.Id)).ToList();

      foreach (var actualItem in allItems)
      {
        var playList = allItemsWithActualItem.SingleOrDefault(x => x.Model.ActualItemId == actualItem.Id);

        if (playList != null)
          playList.Model.ActualItem = actualItem;
      }
    }

    private async void LoadPage()
    {
      var nextPage = AllGeneratedItems.Skip(actualSkip).Take(take);

      await LibraryCollection.AddRange(nextPage);

      actualSkip += take;

      CanLoadMoreItems = actualSkip < AllItems.Count();
      RaisePropertyChanged(nameof(View));
    }

  }
}