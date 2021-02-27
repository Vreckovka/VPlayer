using Logger;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using VCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Library.ViewModels.LibraryViewModels
{
  public class LibraryCollection<TViewModel, TModel> : BindableBase
    where TViewModel : class, INamedEntityViewModel<TModel>
    where TModel : class, INamedEntity
  {
    #region Fields

    protected readonly IStorageManager storageManager;
    private readonly ILogger logger;
    private string actualFilter = "";
    protected IViewModelsFactory ViewModelsFactory { get; }

    private IEnumerable<TViewModel> SortedItems
    {
      get { return Items?.OrderBy(x => x.Name); }
    }

    #endregion Fields

    #region Constructors

    public LibraryCollection(
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      ILogger logger
    )
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

      ViewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      LoadQuery = storageManager.GetRepository<TModel>();

      LoadData = LoadInitilizedDataAsync();
    }

    #endregion Constructors

    #region Properties

    public IEnumerable<TViewModel> FilteredItems { get; set; }
    public ObservableCollection<TViewModel> Items { get; set; }
    public IQueryable<TModel> LoadQuery { get; set; }
    public IObservable<bool> LoadData { get; }
    public bool WasLoaded { get; private set; }

    #endregion Properties

    #region Methods

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

            Recreate();

            WasLoaded = true;
          }

          return true;

        }
        catch (Exception ex)
        {
          logger.Log(ex);
          return false;
        }
      });
    }

    #endregion

    #region GetOrLoadDataAsync

    public IObservable<bool> GetOrLoadDataAsync(IQueryable<TModel> optionalQuery = null)
    {
      return Observable.FromAsync<bool>(async () =>
      {
        if (!WasLoaded)
          return await LoadInitilizedDataAsync(optionalQuery);

        return true;
      });
    }

    #endregion

    #region Add

    public void Add(TModel entity)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        var viewModel = ViewModelsFactory.Create<TViewModel>(entity);
        Items.Add(viewModel);

      });

      Recreate();
    }

    #endregion Add

    #region Remove

    public void Remove(TModel entity)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        if (WasLoaded)
        {
          var originalItem = Items.SingleOrDefault(x => x.ModelId == entity.Id);

          if (originalItem != null)
          {
            Items.Remove(originalItem);
            Recreate();
          }
          else
          {
          }


        }
      });
    }

    #endregion Remove

    #region Update

    public async void Update(TModel entity)
    {
      if(!WasLoaded)
      {
        await GetOrLoadDataAsync();
      }

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

    #region Recreate

    public void Recreate()
    {
      if (Items != null)
      {
        var generator = new ItemsGenerator<TViewModel>(Items,21);

        FilteredItems = new VirtualList<TViewModel>(generator);
      }
    }


    #endregion Filter

    #region Filter

    public void Filter(string name)
    {
      if (!string.IsNullOrEmpty(name))
      {
        FilteredItems = Items.Where(x => x.Name.ToLower().Contains(name) || x.Name.Similarity(name) > 0.8);
      }
      else
      {
        Recreate();
      }
    }

    #endregion

    #endregion
  }
}