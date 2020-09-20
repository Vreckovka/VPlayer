using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VCore;
using VCore.Annotations;
using VCore.Factories;
using VCore.Helpers;
using VCore.Interfaces.ViewModels;
using VCore.Modularity.Events;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels
{
  public abstract class PlayableItemsViewModel<TView, TViewModel, TModel> : 
    RegionViewModel<TView>, INavigationItem, ICollectionViewModel<TViewModel, TModel>, IPlayableItemsViewModel
    where TView : class, IView
    where TViewModel : class, INamedEntityViewModel<TModel>
    where TModel : class, INamedEntity
  {
    #region Fields

    protected readonly IStorageManager storageManager;
    protected readonly IViewModelsFactory viewModelsFactory;

    #endregion Fields

    #region Constructors

    public PlayableItemsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<TViewModel, TModel> libraryCollection) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      LibraryCollection = libraryCollection ?? throw new ArgumentNullException(nameof(libraryCollection));
    }

    #endregion Constructors

    #region Properties

    public abstract override bool ContainsNestedRegions { get; }
    public abstract string Header { get; }
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

    #endregion Properties


    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      LibraryCollection.LoadQuery = LoadQuery;
    }

    #endregion Initialize

    #region ItemsChanged

    protected virtual void ItemsChanged(ItemChanged<TModel> itemChanged)
    {
      var model = itemChanged.Item;

      switch (itemChanged.Changed)
      {
        case Changed.Added:
          LibraryCollection.Add(model);

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
        this.storageManager.SubscribeToItemChange<TModel>(ItemsChanged).DisposeWith(this);

        storageManager.ActionIsDone.Subscribe((x) =>
        {
          LibraryCollection.Recreate();
          RaisePropertyChanged(nameof(ViewModels));
          RaisePropertyChanged(nameof(View));
        }).DisposeWith(this);

        LibraryCollection.LoadData.Subscribe(_ =>
        {
          RaisePropertyChanged(nameof(ViewModels));
          RaisePropertyChanged(nameof(View));
        }).DisposeWith(this);

        wasSubscribed = true;
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

  public interface IPlayableItemsViewModel
  {
    void Filter(string predictate);
  }
}