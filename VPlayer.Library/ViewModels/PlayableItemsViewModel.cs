using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Interfaces;
using VCore.WPF.Interfaces.ViewModels;
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
    }

    #endregion Constructors

    #region Properties

    public LoadingStatus LoadingStatus { get; }

    public abstract override bool ContainsNestedRegions { get; }
    public LibraryCollection<TViewModel, TModel> LibraryCollection { get; set; }

    public virtual IQueryable<TModel> LoadQuery
    {
      get => LibraryCollection.LoadQuery;
    }

    public abstract override string RegionName { get; }

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
    
    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      LibraryCollection.LoadQuery = LoadQuery;
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

          Application.Current?.Dispatcher?.Invoke(() =>
          {
            RaisePropertyChanged(nameof(View));
            RaisePropertyChanged(nameof(ViewModels));
          });

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
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        LibraryCollection.Update(model);
      });

      var vm = LibraryCollection.Items?.SingleOrDefault(x => x.ModelId == model.Id);

      if(vm != null)
      {
        var newItemUpdatedArgs = new ItemUpdatedEventArgs<TViewModel>()
        {
          Model = vm
        };

        eventAggregator.GetEvent<ItemUpdatedEvent<TViewModel>>().Publish(newItemUpdatedArgs);
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
        Task.Run(() => SubscribeToChanges());
      }
    }

    #endregion

    #region SubscribeToChanges

    private bool wasSubscribed;
    private void SubscribeToChanges()
    {
      if (!wasSubscribed)
      {
        wasSubscribed = true;

        this.storageManager.SubscribeToItemChange<TModel>(ItemsChanged).DisposeWith(this);

        storageManager.ActionIsDone.Subscribe((x) =>
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            LibraryCollection.RequestReloadVirtulizedPlaylist(); 
            RaisePropertyChanged(nameof(ViewModels));
            RaisePropertyChanged(nameof(View));
          });
        }).DisposeWith(this);

        LibraryCollection.LoadData.Subscribe(_ =>
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            RaisePropertyChanged(nameof(ViewModels));
            RaisePropertyChanged(nameof(View));
          });

        }).DisposeWith(this);


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

    #endregion Methods

  }
}