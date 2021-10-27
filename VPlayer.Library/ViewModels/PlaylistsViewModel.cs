using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;

namespace VPlayer.Home.ViewModels
{
  public abstract class PlaylistsViewModel<TView, TViewModel, TModel> : PlayableItemsViewModel<TView, TViewModel, TModel>
    where TView : class, IView
    where TViewModel : class, INamedEntityViewModel<TModel>
    where TModel : class, INamedEntity, IFilePlaylist
  {
    public PlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
      LibraryCollection<TViewModel, TModel> libraryCollection,
      IEventAggregator eventAggregator) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      LoadingStatus = new LoadingStatus()
      {
        ShowProcessCount = false
      };
    }

    protected IEnumerable<TViewModel> AllItems { get; set; }
    protected IEnumerable<TViewModel> AllUserCreatedItems { get; set; }
    protected IEnumerable<TViewModel> AllGeneratedItems { get; set; }

    public override IQueryable<TModel> LoadQuery => base.LoadQuery.OrderByDescending(x => x.LastPlayed);

    public ObservableCollection<TViewModel> ViewCollection
    {
      get
      {
        return LibraryCollection.FilteredItemsCollection;
      }
    }

    #region LoadMoreItems

    private ActionCommand loadMoreItems;

    public ICommand LoadMoreItems
    {
      get
      {
        if (loadMoreItems == null)
        {
          loadMoreItems = new ActionCommand(OnLoadMoreItems);
        }

        return loadMoreItems;
      }
    }

    protected virtual void OnLoadMoreItems()
    {
      LoadPage();
    }


    #endregion

    private int actualSkip = 0;
    private int take = 30;

    protected override void OnDataLoaded()
    {
      AllItems = LibraryCollection.Items.ToList();
      AllUserCreatedItems = LibraryCollection.Items.Where(x => x.Model.IsUserCreated).ToList();
      AllGeneratedItems = LibraryCollection.Items.Where(x => !x.Model.IsUserCreated).ToList();

      var userCreated = AllUserCreatedItems;
      var notSavedPlaylists = AllGeneratedItems.Skip(actualSkip).Take(take);

      LibraryCollection.FilteredItemsCollection = new ObservableCollection<TViewModel>(userCreated.Concat(notSavedPlaylists));

      actualSkip += take;
      take = 10;
    }

    private async void LoadPage()
    {
      var nextPage = AllGeneratedItems.Skip(actualSkip).Take(take);

      await LibraryCollection.AddRange(nextPage);

      actualSkip += take;

      RaisePropertyChanged(nameof(View));
    }

  }
}