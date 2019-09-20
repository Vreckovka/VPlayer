using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Gma.DataStructures.StringSearch;
using PropertyChanged;
using VCore.Factories;
using VCore.Modularity.Interfaces;
using VPlayer.Library.Properties;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using VPlayer.Library.VirtualList;
using VPlayer.Library.VirtualList.VirtualLists;

namespace VPlayer.Library.ViewModels.LibraryViewModels
{
  [AddINotifyPropertyChangedInterface]
  public abstract class LibraryCollection<TModel> : ILibraryCollection<TModel>
  where TModel : class,IPlayableViewModel
  {
    #region Fields

    protected IViewModelsFactory ViewModelsFactory { get; }

    private Trie<TModel> trieItems = new Trie<TModel>();

    private IEnumerable<TModel> SortedItems
    {
      get { return Items?.OrderBy(x => x.Name); }
    }

    #endregion

    #region Constructors

    public LibraryCollection(IViewModelsFactory viewModelsFactory)
    {
      ViewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      LoadData();

      AllItems = new VirtualList<TModel>(new PlayableItemsGenerator<TModel>(SortedItems));

      FilteredItems = AllItems;

      CreateTrieItems(SortedItems);
    }

    #endregion

    #region Properties

    #region FilteredItems

    public VirtualList<TModel> AllItems { get; private set; }
    public VirtualList<TModel> FilteredItems { get; private set; }

    #endregion

    #region Items

    public IEnumerable<TModel> Items { get; set; }

    #endregion

    #endregion

    #region Filter

    public void Filter(string name)
    {
      if (!string.IsNullOrEmpty(name.ToLower()))
        FilteredItems = new VirtualList<TModel>(new PlayableItemsGenerator<TModel>(trieItems.Retrieve(name)));
      else
        FilteredItems = AllItems;
    }

    #endregion

    #region LoadData

    public abstract void LoadData();

    #endregion

    #region CreateTrieItems

    private void CreateTrieItems(IEnumerable<TModel> items)
    {
      trieItems = new Trie<TModel>();

      foreach (var artist in items)
      {
        trieItems.Add(artist.Name.ToLower(), artist);
      }
    }

    #endregion

    public void Add(TModel entity)
    {
      AllItems.Add(entity);
      Filter("");
    }
  }

  public interface ILibraryCollection<TModel> where TModel : class
  {
    VirtualList<TModel> FilteredItems { get; }
    void Filter(string name);

  }
}
