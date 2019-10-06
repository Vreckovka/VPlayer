using Gma.DataStructures.StringSearch;
using Prism.Mvvm;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using VCore.Factories;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.VirtualList;
using VPlayer.Library.VirtualList.VirtualLists;

namespace VPlayer.Library.ViewModels.LibraryViewModels
{
  [AddINotifyPropertyChangedInterface]
  public class LibraryCollection<TViewModel, TModel> : BindableBase
    where TViewModel : class, IPlayableViewModel<TModel>
    where TModel : class, INamedEntity
  {
    #region Fields

    protected readonly IStorageManager storageManager;
    private string actualFilter = "";
    private Trie<TViewModel> trieItems = new Trie<TViewModel>();
    protected IViewModelsFactory ViewModelsFactory { get; }

    private IEnumerable<TViewModel> SortedItems
    {
      get { return Items?.OrderBy(x => x.Name); }
    }

    #endregion Fields

    #region Properties

    public IObservable<bool> LoadData { get; }

    #endregion Properties

    #region Constructors

    public LibraryCollection(
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager
    )
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      ViewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      LoadQuery = storageManager.GetRepository<TModel>();

      LoadData = LoadInitilizedDataAsync().Concat(Initilize());
    }

    #endregion Constructors

    #region Properties

    public VirtualList<TViewModel> FilteredItems { get; set; }
    public ObservableCollection<TViewModel> Items { get; set; }
    public IQueryable<TModel> LoadQuery { get; set; }
    public bool WasLoaded { get; private set; }

    #endregion Properties

    #region Filter

    public void Filter(string name)
    {
      if (!string.IsNullOrEmpty(name.ToLower()))
        FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(trieItems.Retrieve(name)));
      else
        FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(Items));

      actualFilter = name;
    }

    #endregion Filter

    private object batton = new object();

    #region LoadInitilizedData

    public bool LoadInitilizedData()
    {
      try
      {
        lock (batton)
        {
          if (!WasLoaded)
          {
            var query = LoadQuery.AsEnumerable();

            Items = new ObservableCollection<TViewModel>(query.Select(x => ViewModelsFactory.Create<TViewModel>(x)).ToList());
            WasLoaded = true;
          }

          return true;
        }
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.LogException(ex);
        return false;
      }
    }

    protected IObservable<bool> LoadInitilizedDataAsync()
    {
      return Observable.FromAsync<bool>(async () =>
      {
        if (!WasLoaded)
          return await Task.Run(() => { return LoadInitilizedData(); });

        return true;
      });
    }

    #endregion LoadInitilizedData

    #region CreateTrieItems

    private void CreateTrieItems(IEnumerable<TViewModel> items)
    {
      trieItems = new Trie<TViewModel>();

      foreach (var artist in items)
      {
        trieItems.Add(artist.Name.ToLower(), artist);
      }
    }

    #endregion CreateTrieItems

    #region Add

    public void Add(TModel entity)
    {
      Items.Add(ViewModelsFactory.Create<TViewModel>(entity));
      Recreate();
    }

    #endregion Add

    #region Remove

    public void Remove(TModel entity)
    {
      if (WasLoaded)
      {
        var originalItem = Items.Single(x => x.ModelId == entity.Id);

        Items.Remove(originalItem);

        Recreate();
      }
    }

    #endregion Remove

    #region Update

    public void Update(TModel entity)
    {
      var originalItem = Items.Single(x => x.ModelId == entity.Id);

      originalItem.Update(entity);

      if (entity.Name != originalItem.Name)
      {
        Recreate();
      }
    }

    #endregion Update

    #region Initilize

    private IObservable<bool> Initilize()
    {
      return Observable.FromAsync<bool>(async () =>
      {
        return await Task.Run(() =>
        {
          try
          {
            if (Items != null)
            {
              Recreate();

              return true;
            }

            return false;
          }
          catch (Exception)
          {
            return false;
          }
        });
      });
    }

    #endregion Initilize

    #region Recreate

    public void Recreate()
    {
      if (Items != null)
      {
        FilteredItems = new VirtualList<TViewModel>(new PlayableItemsGenerator<TViewModel, TModel>(Items));
      }
    }

    #endregion Recreate
  }
}