using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels
{
  public class LibraryViewModel : RegionViewModel<LibraryView>, INavigationItem
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;

    #endregion Fields

    #region Constructors

    public LibraryViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      NavigationViewModel navigationViewModel,
      IStorageManager storage) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.NavigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => true;
    public string Header => "Library";
    public NavigationViewModel NavigationViewModel { get; set; }
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;

    #endregion Properties

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      var playlists = viewModelsFactory.Create<PlaylistsViewModel>();
      NavigationViewModel.Items.Add(playlists);

      var artistsViewModel = viewModelsFactory.Create<IArtistsViewModel>();
      NavigationViewModel.Items.Add(artistsViewModel);

      var albumsViewModel = viewModelsFactory.Create<IAlbumsViewModel>();
      NavigationViewModel.Items.Add(albumsViewModel);


      NavigationViewModel.Items.First().IsActive = true;
    }

    #endregion Initialize

    #region FinderKeyDown

    private ActionCommand<string> finderKeyDown;

    public ICommand FinderKeyDown
    {
      get
      {
        if (finderKeyDown == null)
        {
          finderKeyDown = new ActionCommand<string>(OnFinderKeyDown);
        }

        return finderKeyDown;
      }
    }

    private void OnFinderKeyDown(string phrase)
    {
      //ActualCollectionViewModel.Filter(phrase);
    }

    #endregion FinderKeyDown

    #region FilesDropped

    private ActionCommand<object> filesDropped;

    public ICommand FilesDropped
    {
      get
      {
        if (filesDropped == null)
        {
          filesDropped = new ActionCommand<object>(OnFilesDropped);
        }

        return filesDropped;
      }
    }

    private async void OnFilesDropped(object files)
    {
      IDataObject data = files as IDataObject;
      if (null == data) return;

      var draggedFiles = data.GetData(DataFormats.FileDrop) as IEnumerable<string>;

      await storage.StoreData(draggedFiles);
    }

    #endregion FilesDropped

    #endregion

  }
}