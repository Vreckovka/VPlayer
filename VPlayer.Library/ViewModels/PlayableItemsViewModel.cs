using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Prism.Events;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Interfaces;
using VCore.WPF.Interfaces.ViewModels;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;

namespace VPlayer.Home.ViewModels
{
  public abstract class PlayableItemsViewModel<TView, TViewModel, TModel> :
    RegionViewModel<TView>, ICollectionViewModel<TViewModel, TModel>, IPlayableItemsViewModel, IFilterable
    where TView : class, IView
    where TViewModel : class, INamedEntityViewModel<TModel>
    where TModel : class, INamedEntity
  {
    #region Fields

    protected readonly IStorageManager storageManager;
    private readonly IEventAggregator eventAggregator;
    protected readonly IViewModelsFactory viewModelsFactory;

    private Subject<string> searchSubject = new Subject<string>();

    #endregion Fields

    #region Constructors

    public PlayableItemsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<TViewModel, TModel> libraryCollection,
      IEventAggregator eventAggregator) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      LibraryCollection = libraryCollection ?? throw new ArgumentNullException(nameof(libraryCollection));

      LoadingStatus = new LoadingStatus();


      LibraryCollection.DataLoadedCallback = OnDataLoaded;
    }

    #endregion Constructors

    #region Properties

    public LoadingStatus LoadingStatus { get; protected set; }
    protected virtual bool SubscribeToPinned { get; } = false;



    public abstract override bool ContainsNestedRegions { get; }
    public LibraryCollection<TViewModel, TModel> LibraryCollection { get; set; }

    public virtual IQueryable<TModel> LoadQuery
    {
      get => LibraryCollection.LoadQuery;
    }

    public abstract override string RegionName { get; }


    private ObservableCollection<PinnedItemViewModel> pinnedItems = new ObservableCollection<PinnedItemViewModel>();
    public ObservableCollection<PinnedItemViewModel> PinnedItems => pinnedItems;

    #region SearchKeyWord

    private string searchKeyWord;

    public string SearchKeyWord
    {
      get { return searchKeyWord; }
      set
      {
        if (value != searchKeyWord)
        {
          searchKeyWord = value;
          searchSubject.OnNext(value);
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ViewModels

    public ICollection<TViewModel> ViewModels
    {
      get { return LibraryCollection.Items; }
    }

    #endregion ViewModels

    #region View

    public virtual IEnumerable<TViewModel> View
    {
      get { return LibraryCollection.FilteredItems; }
    }

    #endregion View

    #endregion

    #region Commands

    #region RefreshData

    private ActionCommand refreshData;

    public ICommand RefreshData
    {
      get
      {
        if (refreshData == null)
        {
          refreshData = new ActionCommand(OnRefreshData);
        }

        return refreshData;
      }
    }

    protected virtual async void OnRefreshData()
    {
      try
      {
        LoadingStatus.IsLoading = true;

        PinnedItems.Clear();
        LibraryCollection.Clear();

        await LibraryCollection.LoadInitilizedDataAsync(LoadQuery);

        RaisePropertyChanged(nameof(View));
        SearchKeyWord = null;
      }
      finally
      {
        LoadingStatus.IsLoading = false;
      }
    }


    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      LibraryCollection.LoadQuery = LoadQuery;
      LibraryCollection.OnRecreate.ObserveOnDispatcher().Subscribe(x =>
      {
        RaisePropertyChanged(nameof(View));
        RaisePropertyChanged(nameof(ViewModels));
      }).DisposeWith(this);

      searchSubject.Throttle(TimeSpan.FromSeconds(0.25)).ObserveOnDispatcher().Subscribe(x => Filter(x)).DisposeWith(this);
    }

    #endregion Initialize

    #region ItemsChanged

    protected virtual async void ItemsChanged(IItemChanged<TModel> itemChanged)
    {
      var model = itemChanged.Item;

      switch (itemChanged.Changed)
      {
        case Changed.Added:
          await LibraryCollection.Add(model);
          break;
        case Changed.Removed:
          OnDeleteItemChange(model);
          break;

        case Changed.Updated:
          OnUpdateItemChange(model);
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region OnUpdate

    protected virtual void OnUpdateItemChange(TModel model)
    {
      try
      {
        Application.Current?.Dispatcher?.Invoke(() =>
         {
           LibraryCollection.Update(model);
         });

        var vm = LibraryCollection.Items?.SingleOrDefault(x => x.ModelId == model.Id);

        if (vm != null)
        {
          var newItemUpdatedArgs = new ItemUpdatedEventArgs<TViewModel>()
          {
            Model = vm
          };

          eventAggregator.GetEvent<ItemUpdatedEvent<TViewModel>>().Publish(newItemUpdatedArgs);
        }
      }
      catch (Exception ex)
      {
      }
    }

    #endregion

    #region OnDeleteItemChange

    protected virtual void OnDeleteItemChange(TModel model)
    {
      LibraryCollection.Remove(model);

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        RaisePropertyChanged(nameof(View));
        RaisePropertyChanged(nameof(ViewModels));
      });
    }

    #endregion

    #region OnActivation


    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        SubscribeToChanges();
      }
    }

    #endregion

    #region SubscribeToChanges

    private bool wasSubscribed;
    private void SubscribeToChanges()
    {
      if (!wasSubscribed)
      {
        LoadingStatus.IsLoading = true;

        Task.Run(() =>
        {

          wasSubscribed = true;

          this.storageManager.SubscribeToItemChange<TModel>(ItemsChanged).DisposeWith(this);

          if (SubscribeToPinned)
            this.storageManager.SubscribeToItemChange<PinnedItem>(OnPinnedItemChanged).DisposeWith(this);

          storageManager.ActionIsDone.Subscribe((x) =>
            {
              VSynchronizationContext.PostOnUIThread(() =>
              {
                LibraryCollection.RequestReloadVirtulizedPlaylist();
                RaisePropertyChanged(nameof(ViewModels));
                RaisePropertyChanged(nameof(View));
              });
            }).DisposeWith(this);

          LibraryCollection.LoadData.Subscribe(_ =>
            {
              VSynchronizationContext.PostOnUIThread(() =>
              {
                LoadingStatus.IsLoading = false;
                RaisePropertyChanged(nameof(ViewModels));
                RaisePropertyChanged(nameof(View));
              });

            }).DisposeWith(this);

          if(LibraryCollection.WasLoaded)
          {
            LoadingStatus.IsLoading = false;
          }
        });
      }
    }

    #endregion

    #region GetViewModelsAsync

    public async Task<ICollection<TViewModel>> GetViewModelsAsync(IQueryable<TModel> optionalQuery = null)
    {
      if (!LibraryCollection.WasLoaded)
      {
        SubscribeToChanges();

        var result = await LibraryCollection.GetOrLoadDataAsync(optionalQuery);

        if (!result)
        {
          throw new Exception("Data was not loaded");
        }
      }

      return LibraryCollection.Items;
    }

    #endregion

    protected virtual void OnDataLoaded()
    {
    }

    #region RecreateCollection

    public void RecreateCollection()
    {
      if (LibraryCollection.WasLoaded)
      {
        LibraryCollection.Recreate();

        regionProvider.RefreshView(Guid);

        GC.Collect();
      }
    }

    #endregion

    #region Filter

    public void Filter(string predictate)
    {
      LibraryCollection.Filter(predictate);

      RaisePropertyChanged(nameof(View));
    }

    #endregion

    #region OnPinnedItemChanged

    private void OnPinnedItemChanged(IItemChanged<PinnedItem> itemChanged)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        switch (itemChanged.Changed)
        {
          case Changed.Added:
            SetupNewPinnedItem(itemChanged.Item);
            break;
          case Changed.Removed:
            {
              var existing = PinnedItems.SingleOrDefault(x => x.Model.Id == itemChanged.Item.Id);

              if (existing != null)
                PinnedItems.Remove(existing);
            }
            break;
          case Changed.Updated:
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      });
    }

    #endregion

    protected abstract void SetupNewPinnedItem(PinnedItem pinnedItem);

    #endregion Methods

  }
}