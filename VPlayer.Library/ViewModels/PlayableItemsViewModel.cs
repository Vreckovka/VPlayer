using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using VCore.Factories;
using VCore.Modularity.Events;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels
{
  public abstract class PlayableItemsViewModel<TView, TViewModel, TModel> : RegionViewModel<TView>, INavigationItem
    where TView : class, IView
    where TViewModel : class, IPlayableViewModel<TModel>
    where TModel : class, INamedEntity
  {
    #region Fields

    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;

    #endregion Fields

    #region Constructors

    public PlayableItemsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<TViewModel, TModel> libraryCollection) : base(regionProvider)
    {
      if (viewModelsFactory == null) throw new ArgumentNullException(nameof(viewModelsFactory));
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
      set => LibraryCollection.LoadQuery = value;
    }

    public abstract override string RegionName { get; }

    #region ViewModels

    public ICollection<TViewModel> ViewModels
    {
      get { return LibraryCollection.Items; }
    }

    #endregion ViewModels

    #region View

    public ICollection<TViewModel> View
    {
      get { return LibraryCollection.FilteredItems; }
    }

    #endregion View

    #endregion Properties

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      LibraryCollection.LoadQuery = LoadQuery;
    }

    #endregion Initialize

    #region ItemsChanged

    private void ItemsChanged(ItemChanged itemChanged)
    {
      switch (itemChanged.Changed)
      {
        case Changed.Added:
          LibraryCollection.Add((TModel)itemChanged.Item);
          break;

        case Changed.Removed:
          LibraryCollection.Remove((TModel)itemChanged.Item);
          break;

        case Changed.Updated:
          LibraryCollection.Update((TModel)itemChanged.Item);
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }

      RaisePropertyChanged(nameof(View));
      RaisePropertyChanged(nameof(ViewModels));
    }

    #endregion ItemsChanged

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(TModel)).Subscribe(ItemsChanged);

        if (!LibraryCollection.WasLoaded)
        {
          LibraryCollection.LoadData.Subscribe(_ =>
          {
            RaisePropertyChanged(nameof(ViewModels));
            RaisePropertyChanged(nameof(View));
          });
        }
      }
    }

    #endregion OnActivation
  }
}