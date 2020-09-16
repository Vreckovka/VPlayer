using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VCore.Factories;
using VCore.Interfaces.ViewModels;
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
  public abstract class PlayableItemsViewModel<TView, TViewModel, TModel> : RegionViewModel<TView>, INavigationItem, ICollectionViewModel<TViewModel, TModel>
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
    }

    public abstract override string RegionName { get; }

    #region ViewModels

    public ICollection<TViewModel> ViewModels
    {
      get { return LibraryCollection.Items; }
    }

    #endregion ViewModels

    #region View

    public virtual ICollection<TViewModel> View
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

    protected virtual void ItemsChanged(ItemChanged itemChanged)
    {
      if (itemChanged.Item is TModel model)
      {
        switch (itemChanged.Changed)
        {
          case Changed.Added:
            LibraryCollection.Add(model);

            Application.Current.Dispatcher.Invoke(() =>
            {
              RaisePropertyChanged(nameof(View));
              RaisePropertyChanged(nameof(ViewModels));
            });

            break;
          case Changed.Removed:
            OnDeleteItemChange(model);
            break;

          case Changed.Updated:
            Application.Current.Dispatcher.Invoke(() =>
            {
              LibraryCollection.Update(model);
            });
            break;

          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }

    #endregion

    #region OnDeleteItemChange

    protected virtual void OnDeleteItemChange(TModel model)
    {
      LibraryCollection.Remove(model);

      Application.Current.Dispatcher.Invoke(() =>
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
        this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(TModel)).Subscribe(ItemsChanged);
        storageManager.ActionIsDone.Subscribe((x) =>
        {
          LibraryCollection.Recreate();
          RaisePropertyChanged(nameof(ViewModels));
          RaisePropertyChanged(nameof(View));
        });

        LibraryCollection.LoadData.Subscribe(_ =>
        {
          RaisePropertyChanged(nameof(ViewModels));
          RaisePropertyChanged(nameof(View));
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

    #endregion Methods

  }
}