using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.FileBrowser;
using VPlayer.Library.ViewModels.TvShows;
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
      IVPlayerRegionProvider regionProvider,
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

    #region ActualSearch

    private ReplaySubject<string> actualSearchSubject;
    private string actualSearch;
    public string ActualSearch
    {
      get { return actualSearch; }
      set
      {
        if (value != actualSearch)
        {
          actualSearch = value;

          actualSearchSubject.OnNext(actualSearch);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion Properties

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      actualSearchSubject = new ReplaySubject<string>(1);
      actualSearchSubject.DisposeWith(this);

      actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(Filter).DisposeWith(this);

    }

    #endregion Initialize

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        var songPlaylists = viewModelsFactory.Create<SongPlaylistsViewModel>();
        var artistsViewModel = viewModelsFactory.Create<IArtistsViewModel>();
        var albumsViewModel = viewModelsFactory.Create<IAlbumsViewModel>();
        var tvShowPlaylistsViewModel = viewModelsFactory.Create<VideoPlaylistsViewModel>();
        var tvShowsViewModel = viewModelsFactory.Create<TvShowsViewModel>();
        var fileBrowser = viewModelsFactory.Create<FileBrowserViewModel>();

        Application.Current?.Dispatcher?.Invoke(() =>
        {
          NavigationViewModel.Items.Add(new NavigationItem(songPlaylists)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/song-playlist.png"
          });

          NavigationViewModel.Items.Add(new NavigationItem(tvShowPlaylistsViewModel)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/tvshow-playlist.png"
          });

          NavigationViewModel.Items.Add(new NavigationItem(artistsViewModel)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/singer.png"
          });

          NavigationViewModel.Items.Add(new NavigationItem(albumsViewModel)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/music-album.png"
          });

          NavigationViewModel.Items.Add(new NavigationItem(tvShowsViewModel)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/tvshow.png"
          });

          NavigationViewModel.Items.Add(new NavigationItem(fileBrowser)
          {
            ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/browser.png"
          });

          NavigationViewModel.Items.First().IsActive = true;
        });


      }
    }

    #endregion

    #region Filter

    private void Filter(string phrase)
    {
      var acutal = NavigationViewModel.Items.SingleOrDefault(x => x.IsActive);

      if (acutal != null && acutal.navigationItem is IFilterable filterable)
      {
        filterable.Filter(phrase);
      }
    }

    #endregion 

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