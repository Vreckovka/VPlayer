using Gma.DataStructures.StringSearch;
using Prism.Mvvm;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
    where TViewModel : class, INamedEntityViewModel<TModel>
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

    #region LoadInitilizedData

    public IObservable<bool> LoadInitilizedDataAsync(IQueryable<TModel> optionalQuery = null)
    {
      return Observable.FromAsync<bool>(async () =>
      {
        try
        {
          if (!WasLoaded)
          {
            List<TModel> data;
            if (optionalQuery == null)
              //Need Enumerable for ViewModelsFactory.Create
              data = await LoadQuery.ToListAsync();
            else
              data = await optionalQuery.ToListAsync();

            Items = new ObservableCollection<TViewModel>(data.Select(x => ViewModelsFactory.Create<TViewModel>(x)).ToList());

            WasLoaded = true;
          }

          return true;

        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(ex);
          return false;
        }
      });
    }

    public IObservable<bool> GetOrLoadDataAsync(IQueryable<TModel> optionalQuery)
    {
      return Observable.FromAsync<bool>(async () =>
      {
        if (!WasLoaded)
          return await LoadInitilizedDataAsync(optionalQuery);

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
      Application.Current.Dispatcher.Invoke(() =>
      {
        var viewModel = ViewModelsFactory.Create<TViewModel>(entity);
        Items.Add(viewModel);

      });

      //Recreate();
    }

    #endregion Add

    #region Remove

    public void Remove(TModel entity)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (WasLoaded)
        {
          var originalItem = Items.SingleOrDefault(x => x.ModelId == entity.Id);

          if (originalItem != null)
          {
            Items.Remove(originalItem);
          }

          Recreate();
        }
      });
    }

    #endregion Remove

    #region Update

    public void Update(TModel entity)
    {
      var originalItem = Items.SingleOrDefault(x => x.ModelId == entity.Id);

      if (originalItem != null)
      {
        originalItem.Update(entity);

        if (entity.Name != originalItem.Name)
        {
          //Recreate();
        }
      }

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