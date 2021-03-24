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
using VPlayer.Library.ViewModels.TvShows;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels
{
  public static class IconPathData
  {
    public const string MusicIcon = "M328.712,264.539c12.928-21.632,21.504-48.992,23.168-76.064c1.056-17.376-2.816-35.616-11.2-52.768  c-13.152-26.944-35.744-42.08-57.568-56.704c-16.288-10.912-31.68-21.216-42.56-35.936l-1.952-2.624  c-6.432-8.64-13.696-18.432-14.848-26.656c-1.152-8.32-8.704-14.24-16.96-13.76c-8.384,0.576-14.88,7.52-14.88,15.936v285.12  c-13.408-8.128-29.92-13.12-48-13.12c-44.096,0-80,28.704-80,64s35.904,64,80,64s80-28.704,80-64V165.467  c24.032,9.184,63.36,32.576,74.176,87.2c-2.016,2.976-3.936,6.176-6.176,8.736c-5.856,6.624-5.216,16.736,1.44,22.56  c6.592,5.888,16.704,5.184,22.56-1.44c4.288-4.864,8.096-10.56,11.744-16.512C328.04,265.563,328.393,265.083,328.712,264.539z";
  }
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

      actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(Filter).DisposeWith(this);

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
        var tvShowPlaylistsViewModel = viewModelsFactory.Create<TvShowPlaylistsViewModel>();
        var tvShowsViewModel = viewModelsFactory.Create<TvShowsViewModel>();

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

          NavigationViewModel.Items.First().IsActive = true;
        });


      }
    }

    #endregion

    #region Filter

    private void Filter(string phrase)
    {
      var acutal = NavigationViewModel.Items.SingleOrDefault(x => x.IsActive);

      if (acutal != null && acutal.navigationItem is IPlayableItemsViewModel playableItemsViewModel)
      {
        playableItemsViewModel.Filter(phrase);
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