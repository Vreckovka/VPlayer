using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using VCore.Factories;
using VCore.Interfaces.ViewModels;
using VCore.Modularity.Events;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels
{
  public abstract class PlayableItemsViewModel<TView, TViewModel, TModel> : RegionViewModel<TView>, INavigationItem
      where TView : class, IView
      where TViewModel : class, IPlayableViewModel<TModel>
      where TModel : class, INamedEntity
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;

    #endregion

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

      this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(TModel)).Subscribe(ItemsChanged);
    }

    #endregion

    #region Properties

    public abstract override string RegionName { get; }
    public abstract override bool ContainsNestedRegions { get; }
    public abstract string Header { get; }

    public virtual IQueryable<TModel> LoadQuery
    {
      get => LibraryCollection.LoadQuery;
      set => LibraryCollection.LoadQuery = value;
    }


    public LibraryCollection<TViewModel, TModel> LibraryCollection { get; set; }

    #region ViewModels

    public ICollection<TViewModel> ViewModels
    {
      get
      {
        if (!LibraryCollection.WasLoaded)
        {
          LibraryCollection.LoadDataImmediately();
        }

        return LibraryCollection.Items;
      }
    }

    #endregion

    #region View

    public ICollection<TViewModel> View
    {
      get
      {
        if (!LibraryCollection.WasLoaded)
        {
          LibraryCollection.LoadDataImmediately();
        }

        return LibraryCollection.FilteredItems;
      }
    }

    #endregion

    #endregion

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      LibraryCollection.LoadQuery = LoadQuery;
    }

    #endregion

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

      RaisePropertyChanged(nameof(ViewModels));
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation && !LibraryCollection.WasLoaded)
      {
        LibraryCollection.LoadData.Subscribe(_ =>
        {
          RaisePropertyChanged(nameof(ViewModels));
          RaisePropertyChanged(nameof(View));
        });
      }
    }

    #endregion
  }
}