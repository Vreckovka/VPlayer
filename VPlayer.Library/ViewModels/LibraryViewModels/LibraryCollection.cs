using VPLayer.Domain.Text;
using System;
using VPLayer.Domain.Diagnostics;
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

      ResetLoadQuery();
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

    private IQueryable<TModel> customQuery;
    private Func<IQueryable<TModel>, IQueryable<TModel>> queryTransform;
    private readonly object lookupGate=new object();
    private readonly Dictionary<int,TViewModel> lookupViews=new Dictionary<int,TViewModel>();
    private long queryGeneration;
    private Lazy<IQueryable<TModel>> deferredQuery;
    public IQueryable<TModel> LoadQuery
    {
      get => deferredQuery.Value;
      set { customQuery = value; ResetLoadQuery(); }
    }

    public void ConfigureQuery(Func<IQueryable<TModel>, IQueryable<TModel>> transform)
    {
      queryTransform = transform ?? throw new ArgumentNullException(nameof(transform));
      ResetLoadQuery();
    }

    private void ResetLoadQuery()
    {
      lock(lookupGate) {lookupViews.Clear();queryGeneration++;}
      var query = customQuery;
      var transform = queryTransform;
      deferredQuery = new Lazy<IQueryable<TModel>>(() =>
      {
        using var measurement = StartupMeasurements.MeasureLibrary("repository setup", typeof(TModel));
        var root = query ?? storageManager.GetTempRepository<TModel>();
        return transform == null ? root :
          transform(root) ?? throw new InvalidOperationException("Query configuration returned null.");
      }, LazyThreadSafetyMode.ExecutionAndPublication);
    }
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
        await semaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
          if (WasLoaded) return true;
          long generation;
          lock(lookupGate) generation=queryGeneration;

          var vms = await Task.Run(async () =>
          {
            List<TModel> data;
            using (StartupMeasurements.MeasureLibrary("query", typeof(TModel)))
              data = await (optionalQuery ?? LoadQuery).ToListAsync().ConfigureAwait(false);
            Dictionary<int,TViewModel> cachedViews;
            lock(lookupGate)
            {
              if(generation!=queryGeneration) return null;
              cachedViews=new Dictionary<int,TViewModel>(lookupViews);
            }
            using var construction = StartupMeasurements.MeasureLibrary("view model construction", typeof(TModel));
            var views=new List<TViewModel>(data.Count);
            var refresh=new List<(TViewModel View,TModel Model)>(cachedViews.Count);
            foreach(var model in data)
            {
              if(cachedViews.TryGetValue(model.Id,out var cached))
              {
                views.Add(cached);
                refresh.Add((cached,model));
              }
              else views.Add(ViewModelsFactory.Create<TViewModel>(model));
            }
            return new {Views=views,Refresh=refresh};
          }).ConfigureAwait(false);

          if(vms==null) return false;
          bool published=false;
          using (StartupMeasurements.MeasureLibrary("UI dispatch and publication", typeof(TModel)))
          await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
          {
            using var publication = StartupMeasurements.MeasureLibrary("UI publication", typeof(TModel));
            lock(lookupGate)
            {
              if(generation!=queryGeneration)
              {
                var shared=new HashSet<TViewModel>(vms.Refresh.Select(x=>x.View));
                foreach(var view in vms.Views)
                  if(!shared.Contains(view)) (view as IDisposable)?.Dispose();
                return;
              }
            }
            foreach(var row in vms.Refresh) row.View.RefreshModel(row.Model);
            var views=vms.Views;
            Items = new RxObservableCollection<TViewModel>(views);
            FilteredItemsCollection = new ObservableCollection<TViewModel>(
              MaxTake.HasValue ? views.Take(MaxTake.Value) : views);
            Items.CollectionChanged += Items_CollectionChanged;
            Recreate();
            WasLoaded = true;
            published=true;
          }).ConfigureAwait(false);

          if(!published) return false;

          // Existing callbacks may perform database queries; keep those off the UI thread.
          using (StartupMeasurements.MeasureLibrary("post-load callback", typeof(TModel)))
          await Task.Run(() => DataLoadedCallback?.Invoke()).ConfigureAwait(false);
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
      });
    }
    private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
    }

    #endregion

    // Warm only requested relationships; keep the full library unloaded.
    public async Task PrepareViewModelsAsync(IEnumerable<int> modelIds)
    {
      if(modelIds==null) throw new ArgumentNullException(nameof(modelIds));
      var ids=modelIds.Where(id=>id>0).Distinct().ToArray();
      if(WasLoaded || ids.Length==0) return;
      await semaphoreSlim.WaitAsync().ConfigureAwait(false);
      try
      {
        if(WasLoaded) return;
        long generation;
        lock(lookupGate)
        {
          ids=ids.Where(id=>!lookupViews.ContainsKey(id)).ToArray();
          generation=queryGeneration;
        }
        if(ids.Length==0) return;
        using var measurement=VPLayer.Domain.Diagnostics.StartupMeasurements.MeasureLibrary("batch item lookup",typeof(TModel));
        await Task.Run(async () =>
        {
          // Bound query width, including callers with very large ID sets.
          for(int offset=0;offset<ids.Length;offset+=256)
          {
            var batch=ids.Skip(offset).Take(256).ToArray();
            var models=await LoadQuery.Where(model=>batch.Contains(model.Id)).ToListAsync().ConfigureAwait(false);
            lock(lookupGate) {if(generation!=queryGeneration) return;}
            var views=models.Select(model=>ViewModelsFactory.Create<TViewModel>(model)).ToArray();
            lock(lookupGate)
            {
              if(generation!=queryGeneration) return;
              foreach(var view in views) lookupViews[view.ModelId]=view;
            }
          }
        }).ConfigureAwait(false);
      }
      finally {semaphoreSlim.Release();}
    }

    public async Task<TViewModel> GetViewModelAsync(int modelId)
    {
      if(modelId<=0) return null;
      // Song initialization may synchronously wait on a worker; cached reads
      // must not schedule a round trip to the blocked UI dispatcher.
      if(WasLoaded) return Items.SingleOrDefault(x=>x.ModelId==modelId);
      lock(lookupGate) {if(lookupViews.TryGetValue(modelId,out var ready)) return ready;}
      await semaphoreSlim.WaitAsync().ConfigureAwait(false);
      try
      {
        if(WasLoaded) return Items.SingleOrDefault(x=>x.ModelId==modelId);

        long generation;
        lock(lookupGate)
        {
          if(lookupViews.TryGetValue(modelId,out var cached)) return cached;
          generation=queryGeneration;
        }
        using var measurement=StartupMeasurements.MeasureLibrary("single item lookup",typeof(TModel));
        var model=await Task.Run(()=>LoadQuery.SingleOrDefaultAsync(x=>x.Id==modelId)).ConfigureAwait(false);
        if(model==null) return null;
        var view=ViewModelsFactory.Create<TViewModel>(model);
        lock(lookupGate)
        {
          if(generation==queryGeneration) lookupViews[modelId]=view;
        }
        return view;
      }
      finally {semaphoreSlim.Release();}
    }
    public async Task RefreshCachedAsync(int modelId,bool removed=false)
    {
      await semaphoreSlim.WaitAsync().ConfigureAwait(false);
      try
      {
        TViewModel view;
        long generation;
        lock(lookupGate)
        {
          if(!lookupViews.TryGetValue(modelId,out view)) return;
          if(removed) {lookupViews.Remove(modelId);return;}
          generation=queryGeneration;
        }
        var model=await Task.Run(()=>LoadQuery.SingleOrDefaultAsync(x=>x.Id==modelId)).ConfigureAwait(false);
        await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
        {
          lock(lookupGate)
          {
            if(generation!=queryGeneration) return;
            if(model==null) lookupViews.Remove(modelId);
            else view.RefreshModel(model);
          }
        }).ConfigureAwait(false);
      }
      catch(Exception error) {logger.Log(error);}
      finally {semaphoreSlim.Release();}
    }

    public async Task RefreshCachedAsync(Func<TViewModel,bool> predicate,bool removed=false)
    {
      int[] ids;
      lock(lookupGate) ids=lookupViews.Where(x=>predicate(x.Value)).Select(x=>x.Key).ToArray();
      foreach(var id in ids) await RefreshCachedAsync(id,removed).ConfigureAwait(false);
    }
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
        var sorted = Items.OrderBy(x => x?.Name);
        var snapshot = MaxTake.HasValue ? sorted.Take(MaxTake.Value).ToArray() : sorted.ToArray();
        FilteredItems = snapshot;
        recreateSubject.OnNext(Unit.Default);
      }
    }

    #endregion 

    #region Filter

    public void Filter(string predicated)
    {
      if (!string.IsNullOrEmpty(predicated))
      {
        var normalized = predicated.ToLowerInvariant();
        FilteredItems = ((IEnumerable<TViewModel>)Items ?? Enumerable.Empty<TViewModel>()).Where(x =>
        {
          var name = x?.Name;
          return name != null && (name.IndexOf(predicated, StringComparison.OrdinalIgnoreCase) >= 0 ||
            FuzzySearch.IsSimilar(name,normalized));
        }).ToList();
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
          VSynchronizationContext.PostOnUIThread(() =>
          {
            stopwatchReloadVirtulizedPlaylist = null;
            Recreate();
          });
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
      LoadQuery = null;
      FilteredItemsCollection?.Clear();
      WasLoaded = false;
    }

    #endregion

    #endregion
  }
}
