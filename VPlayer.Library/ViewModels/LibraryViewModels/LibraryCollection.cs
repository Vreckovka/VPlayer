using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Gma.DataStructures.StringSearch;
using Prism.Mvvm;
using PropertyChanged;
using VCore.Factories;
using VCore.Modularity.Interfaces;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.ViewModels;
using VPlayer.Library.Properties;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using VPlayer.Library.VirtualList;
using VPlayer.Library.VirtualList.VirtualLists;

namespace VPlayer.Library.ViewModels.LibraryViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class LibraryCollection<TViewModel, TModel>
    where TViewModel : class, IPlayableViewModel<TModel>
    where TModel : class, INamedEntity
    {
        #region Fields

        protected readonly IStorageManager storageManager;
        protected IViewModelsFactory ViewModelsFactory { get; }

        private Trie<TViewModel> trieItems = new Trie<TViewModel>();
        private string actualFilter = "";

        private IEnumerable<TViewModel> SortedItems
        {
            get { return Items?.OrderBy(x => x.Name); }
        }

        #endregion

        public IObservable<bool> LoadData;

        #region Constructors

        public LibraryCollection(IViewModelsFactory viewModelsFactory, [VCore.Annotations.NotNull] IStorageManager storageManager)
        {
            this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            ViewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

            LoadQuery = storageManager.GetRepository<TModel>();

            LoadData = LoadInitilizedData().Concat(Initilize());
        }

        #endregion

        #region Properties

        public ObservableCollection<TViewModel> Items { get; set; }
        public VirtualList<TViewModel> FilteredItems { get; set; }

        public IQueryable<TModel> LoadQuery { get; set; }

        #endregion

        #region Filter

        public void Filter(string name)
        {
            if (!string.IsNullOrEmpty(name.ToLower()))
                FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(trieItems.Retrieve(name)));
            else
                FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(Items));

            actualFilter = name;
        }

        #endregion

        #region LoadInitilizedData

        protected IObservable<bool> LoadInitilizedData()
        {
            return Observable.FromAsync<bool>(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                      
                        var query = LoadQuery.ToList();

                        Items = new ObservableCollection<TViewModel>(query.Select(x => ViewModelsFactory.Create<TViewModel>(x)).ToList());

                        return true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                });
            });
        }

        #endregion

        #region CreateTrieItems

        private void CreateTrieItems(IEnumerable<TViewModel> items)
        {
            trieItems = new Trie<TViewModel>();

            foreach (var artist in items)
            {
                trieItems.Add(artist.Name.ToLower(), artist);
            }
        }

        #endregion

        #region Add

        public void Add(TModel entity)
        {
            Items.Add(ViewModelsFactory.Create<TViewModel>(entity));
            Recreate();
        }

        #endregion

        #region Remove

        public void Remove(TModel entity)
        {
            var originalItem = Items.Single(x => x.ModelId == entity.Id);

            Items.Remove(originalItem);

            Recreate();
        }

        #endregion

        #region Update

        public void Update(TModel entity)
        {

            var originalItem = Items.Single(x => x.ModelId == entity.Id);

            originalItem.Update(entity);

            Recreate();
        }

        #endregion

        #region Initilize

        private IObservable<bool> Initilize()
        {
            return Observable.FromAsync<bool>(async () =>
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        Recreate();

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
            });
        }

        #endregion

        #region Recreate

        public void Recreate()
        {
            FilteredItems = FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(Items));

            CreateTrieItems(SortedItems);

        } 

        #endregion
    }
}
