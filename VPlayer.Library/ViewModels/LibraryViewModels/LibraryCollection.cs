using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Mvvm;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Home.ViewModels.LibraryViewModels
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
    private Subject<Unit> recreateSubject = new Subject<Unit>();

    private IEnumerable<TViewModel> SortedItems
    {
      get { return Items?.OrderBy(x => x.Name); }
    }

    public int? MaxTake { get; set; }

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

      LoadQuery = storageManager.GetTempRepository<TModel>();
      LoadData = LoadInitilizedDataAsync();
    }

    #endregion Constructors

    #region Properties

    #region FilteredItems

    private IEnumerable<TViewModel> filteredItems;

    public IEnumerable<TViewModel> FilteredItems
    {
      get { return filteredItems; }
      set
      {
        if (value != filteredItems)
        {
          filteredItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Items

    private RxObservableCollection<TViewModel> items;

    public RxObservableCollection<TViewModel> Items
    {
      get { return items; }
      set
      {
        if (value != items)
        {
          items = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FilteredItemsCollection

    private ObservableCollection<TViewModel> filteredItemsCollection;

    public ObservableCollection<TViewModel> FilteredItemsCollection
    {
      get { return filteredItemsCollection; }
      set
      {
        if (value != filteredItemsCollection)
        {
          filteredItemsCollection = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public IQueryable<TModel> LoadQuery { get; set; }
    public IObservable<bool> LoadData { get; }
    public bool WasLoaded { get; private set; }
    public Action DataLoadedCallback { get; set; }

    public IObservable<Unit> OnRecreate
    {
      get
      {
        return recreateSubject.AsObservable();
      }
    }

    #endregion Properties

    #region Methods

    #region LoadInitilizedData

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    public IObservable<bool> LoadInitilizedDataAsync(IQueryable<TModel> optionalQuery = null)
    {
      return Observable.FromAsync(async () =>
      {
        return await Task.Run(async () =>
          {
            try
            {
              await semaphoreSlim.WaitAsync().ConfigureAwait(false);

              if (!WasLoaded)
              {
                List<TModel> data;
                if (optionalQuery == null)
                  //Need Enumerable for ViewModelsFactory.Create
                  data = await LoadQuery.ToListAsync();
                else
                  data = await optionalQuery.ToListAsync();

                var vms = data.Select(x => ViewModelsFactory.Create<TViewModel>(x)).ToList();

                Items = new RxObservableCollection<TViewModel>(vms);

                if(MaxTake != null)
                {
                  FilteredItemsCollection = new ObservableCollection<TViewModel>(vms.Take(MaxTake.Value));
                }
                else
                {
                  FilteredItemsCollection = new ObservableCollection<TViewModel>(vms);
                }
             

                Items.CollectionChanged += Items_CollectionChanged;
                Recreate();

                WasLoaded = true;

                Task.Run(() => DataLoadedCallback?.Invoke());
              }

              return true;

            }
            catch (Exception ex)
            {
              logger.Log(ex);
              return false;
            }
            finally
            {
              semaphoreSlim.Release();
            }
          }).ConfigureAwait(false);
      });
    }

    private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
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

    public async Task Add(TModel entity)
    {
      var viewModel = ViewModelsFactory.Create<TViewModel>(entity);

      if (!WasLoaded)
      {
        await LoadInitilizedDataAsync();
      }

      VSynchronizationContext.PostOnUIThread(() =>
      {
        Items.Add(viewModel);
        FilteredItemsCollection.Add(viewModel);
      });

      RequestReloadVirtulizedPlaylist();
    }

    public async Task AddRange(IEnumerable<TViewModel> entities)
    {
      var list = entities.ToList();

      if (!WasLoaded)
      {
        await LoadInitilizedDataAsync();
      }

      VSynchronizationContext.PostOnUIThread(() =>
      {
        Items.AddRange(list);
        FilteredItemsCollection.AddRange(list);
      });

      RequestReloadVirtulizedPlaylist();
    }

    #endregion 

    #region Remove

    public void Remove(TModel entity)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        if (WasLoaded)
        {
          var items = Items.Where(x => x.ModelId == entity.Id).ToList();

          foreach (var item in items)
          {
            Items.Remove(item);
            FilteredItemsCollection.Remove(item);
          }

          if (items.Count > 0)
          {
            RequestReloadVirtulizedPlaylist();
          }
        }
      });
    }

    #endregion

    #region Remove

    public void Remove(IEnumerable<TModel> entities)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        if (WasLoaded)
        {
          bool wasChnaged = false;

          foreach (var entity in entities)
          {
            var items = Items.Where(x => x.ModelId == entity.Id).ToList();

            foreach (var item in items)
            {
              Items.Remove(item);
              FilteredItemsCollection.Remove(item);
              wasChnaged = true;
            }
          }

          if (wasChnaged)
          {
            RequestReloadVirtulizedPlaylist();
          }
        }
      });
    }

    #endregion 

    #region Update

    public async void Update(TModel entity)
    {
      if (!WasLoaded)
      {
        await GetOrLoadDataAsync();
      }

      var originalItem = Items.SingleOrDefault(x => x.ModelId == entity.Id);

      if (originalItem != null)
      {
        originalItem.Update(entity);

        RequestReloadVirtulizedPlaylist();
      }

    }

    #endregion 

    #region Recreate

    public void Recreate()
    {
      if (Items != null)
      {
        ItemsGenerator<TViewModel> generator = null;

        if (MaxTake != null)
        {
          generator = new ItemsGenerator<TViewModel>(Items.OrderBy(x => x?.Name).Take(MaxTake.Value), 21);
        }
        else
        {
          generator = new ItemsGenerator<TViewModel>(Items.OrderBy(x => x?.Name), 21);
        }

        FilteredItems = new VirtualList<TViewModel>(generator);

        recreateSubject.OnNext(Unit.Default);
      }
    }

    #endregion 

    #region Filter

    public void Filter(string predicated)
    {
      if (!string.IsNullOrEmpty(predicated))
      {
        FilteredItems = Items.Where(x => x.Name.ToLower().Contains(predicated) || x.Name.Similarity(predicated) > 0.8);
        FilteredItemsCollection = new ObservableCollection<TViewModel>(FilteredItems);
      }
      else
      {
        RequestReloadVirtulizedPlaylist();
      }
    }

    #endregion

    #region ReloadVirtulizedPlaylist

    private Stopwatch stopwatchReloadVirtulizedPlaylist;
    private object batton = new object();
    private SerialDisposable serialDisposable = new SerialDisposable();

    public void RequestReloadVirtulizedPlaylist()
    {
      int dueTime = 1500;
      lock (batton)
      {
        serialDisposable.Disposable = Observable.Timer(TimeSpan.FromMilliseconds(dueTime)).Subscribe((x) =>
        {
          stopwatchReloadVirtulizedPlaylist = null;
          Recreate();
        });

        if (stopwatchReloadVirtulizedPlaylist == null || stopwatchReloadVirtulizedPlaylist.ElapsedMilliseconds > dueTime)
        {
          Recreate();

          serialDisposable.Disposable?.Dispose();
          stopwatchReloadVirtulizedPlaylist = new Stopwatch();
          stopwatchReloadVirtulizedPlaylist.Start();
        }
      }
    }

    #endregion

    #region Clear

    public void Clear()
    {
      Items?.Clear();
      LoadQuery = storageManager.GetTempRepository<TModel>();
      FilteredItemsCollection?.Clear();
      WasLoaded = false;
    }

    #endregion

    #endregion
  }
}