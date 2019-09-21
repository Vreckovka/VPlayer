using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using VCore.Factories;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.VirtualList.VirtualLists;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
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

        public LibraryCollection<TViewModel, TModel> LibraryCollection { get; set; }

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

            if (firstActivation)
            {
                LibraryCollection.LoadData.Subscribe(_ => { RaisePropertyChanged(nameof(ViewModels)); });
            }

        }

        #endregion
    }
}