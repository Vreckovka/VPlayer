using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Gma.DataStructures.StringSearch;
using PropertyChanged;
using VirtualListWithPagingTechnique.VirtualLists;
using VPlayer.Core.Factories;
using VPlayer.Library.Annotations;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.VirtualList;

namespace VPlayer.Library.ViewModels.LibraryViewModels
{
    [AddINotifyPropertyChangedInterface]
    public abstract class LibraryCollection : ILibraryCollection<IPlayableViewModel>, INotifyPropertyChanged
    {
        #region Fields

        protected IViewModelFactory ViewModelFactory { get; }

        private Trie<IPlayableViewModel> trieItems = new Trie<IPlayableViewModel>();

        #region Items

        private IEnumerable<IPlayableViewModel> items;
        public IEnumerable<IPlayableViewModel> Items
        {
            get { return items; }
            set
            {
                if (value != items)
                {
                    items = value;

                    OnPropertyChanged(nameof(Items));
                }
            }
        }

        #endregion

        #region SortedItems

        private IEnumerable<IPlayableViewModel> SortedItems
        {
            get { return Items?.OrderBy(x => x.Name); }
        }

        #endregion

        #endregion

        #region Constructors

        public LibraryCollection(IViewModelFactory viewModelFactory)
        {
            ViewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

            LoadData();

            AllItems = new VirtualList<IPlayableViewModel>(new PlayableItemsGenerator<IPlayableViewModel>(SortedItems));

            PlayableItems = AllItems;

          

            CreateTrieItems(SortedItems);
        }

        #endregion

        #region Properties

        #region PlayableItems

        public VirtualList<IPlayableViewModel> AllItems { get; private set; }
        public VirtualList<IPlayableViewModel> PlayableItems { get; private set; }

        #endregion

        #endregion

        public void Filter(string name)
        {
            if (!string.IsNullOrEmpty(name.ToLower()))
                PlayableItems = new VirtualList<IPlayableViewModel>(new PlayableItemsGenerator<IPlayableViewModel>(trieItems.Retrieve(name)));
            else
                PlayableItems = AllItems;
        }

        protected abstract void LoadData();

        #region CreateTrieItems

        private void CreateTrieItems(IEnumerable<IPlayableViewModel> items)
        {
            trieItems = new Trie<IPlayableViewModel>();

            foreach (var artist in items)
            {
                trieItems.Add(artist.Name.ToLower(), artist);
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface ILibraryCollection<TModel> where TModel : class
    {
        VirtualList<TModel> PlayableItems { get; }
        void Filter(string name);
        
    }
}
