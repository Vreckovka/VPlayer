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
using VCore.WPF.Interfaces;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.IPTV;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.FileBrowser;
using VPlayer.Library.ViewModels.IPTV;
using VPlayer.Library.ViewModels.TvShows;
using VPlayer.Library.Views;
using VPlayer.UPnP.ViewModels;

namespace VPlayer.Library.ViewModels
{
  public class LibraryViewModel : RegionViewModel<LibraryView>, INavigationItem, IFilterable
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
    public string Header => "Home";
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
        InitilizeMenu();
      }
    }

    #endregion

    #region InitilizeMenu

    public void InitilizeMenu()
    {
      var songPlaylists = viewModelsFactory.Create<SongPlaylistsViewModel>();
      var artistsViewModel = viewModelsFactory.Create<IArtistsViewModel>();
      var albumsViewModel = viewModelsFactory.Create<IAlbumsViewModel>();
      var tvShowPlaylistsViewModel = viewModelsFactory.Create<VideoPlaylistsViewModel>();
      var tvShowsViewModel = viewModelsFactory.Create<TvShowsViewModel>();
      var fileBrowser = viewModelsFactory.Create<FileBrowserViewModel>();
      var upnp = viewModelsFactory.Create<UPnPManagerViewModel>();
      var iptvPlaylists = viewModelsFactory.Create<IPTVPlaylistsViewModel>();
      var iptv = viewModelsFactory.Create<IPTVManagerViewModel>();

      NavigationViewModel.Items.Add(new NavigationItem(songPlaylists)
      {
        IconPathData = "M24 95.97h240c13.25 0 24-10.75 24-24s-10.75-24-24-24h-240c-13.25 0-24 10.75-24 24S10.75 95.97 24 95.97zM498.8 6.162c-8.25-6.125-18.69-7.699-28.44-4.699l-112 35.38C345.1 41.09 336 53.34 336 67.34v299.1c-18.12-9.125-40.13-14.5-64-14.5c-61.88 0-112 35.88-112 80C160 476.1 210.1 512 272 512s112-35.88 112-80V195.4l105.6-33.38C503 157.7 512 145.5 512 131.5V31.96C512 21.71 507.1 12.16 498.8 6.162zM272 463.1c-39.75 0-64-20.75-64-31.1c0-11.25 24.25-32 64-32s64 20.75 64 32C336 443.2 311.8 463.1 272 463.1zM464 119.7L384 144.1V79.09l80-25.25V119.7zM24 223.1h240c13.25 0 24-10.75 24-24s-10.75-24-24-24h-240c-13.25 0-24 10.74-24 24S10.75 223.1 24 223.1zM136 303.1h-112c-13.25 0-24 10.75-24 24s10.75 24 24 24h112c13.25 0 24-10.75 24-24S149.3 303.1 136 303.1z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(tvShowPlaylistsViewModel)
      {
        ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/tvshow-playlist.png"
      });

      NavigationViewModel.Items.Add(new NavigationItem(iptvPlaylists)
      {
        IconPathData = "M201.5 344.5l47.48-47.48c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L167.5 310.5L72.06 215C67.11 210.1 60.33 207.7 53.27 208.1C46.3 208.6 39.89 212.2 35.75 217.8C12.36 249.7 0 287.4 0 327.1C0 429.1 82.95 512 184.9 512c39.66 0 77.45-12.38 109.3-35.75c5.641-4.156 9.203-10.53 9.734-17.53c.5313-6.969-2.016-13.84-6.969-18.78L201.5 344.5zM184.9 464C109.4 464 48 402.6 48 327.1c0-19.66 4.109-38.72 12.02-56.22L241.1 452C223.6 459.9 204.6 464 184.9 464zM216 0C202.8 0 192 10.75 192 24S202.8 48 216 48c136.8 0 248 111.3 248 248c0 13.25 10.75 24 24 24S512 309.3 512 296C512 132.8 379.2 0 216 0zM216 104C202.8 104 192 114.8 192 128s10.75 24 24 24c79.41 0 144 64.59 144 144C360 309.3 370.8 320 384 320s24-10.75 24-24C408 190.1 321.9 104 216 104z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(artistsViewModel)
      {
        IconPathData = "M224 256c70.69 0 128-57.31 128-128c0-70.69-57.31-128-128-128S96 57.31 96 128C96 198.7 153.3 256 224 256zM224 48c44.11 0 80 35.89 80 80c0 44.11-35.89 80-80 80S144 172.1 144 128C144 83.89 179.9 48 224 48zM323.6 464H48.99C56.89 400.9 110.8 352 176 352h96c27.91 0 53.46 9.367 74.51 24.59c11.15-11.85 25.33-21.48 41.8-28.44C357.3 320.8 316.6 304 272 304h-96C78.8 304 0 382.8 0 480c0 17.67 14.33 32 32 32h323.2C339.8 498.9 328.8 482.5 323.6 464zM601.7 160.6l-96 19.2C490.8 182.8 480 195.1 480 211.2v161.2C469.9 369.7 459.3 368 448 368c-53.02 0-96 32.23-96 72c0 39.76 42.98 72 96 72s96-32.24 96-72V300.2l70.28-14.05C629.2 283.1 640 269.1 640 254.7V192C640 171.8 621.5 156.7 601.7 160.6z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(albumsViewModel)
      {
        IconPathData = "M227.5 131.2c8.609-1.953 14.03-10.52 12.08-19.14c-1.953-8.609-10.55-13.95-19.14-12.08c-59.7 13.5-107 60.81-120.5 120.5C97.1 229.1 103.4 237.7 112 239.6C113.2 239.9 114.4 240 115.6 240c7.312 0 13.91-5.047 15.59-12.47C141.1 179.8 179.8 141.1 227.5 131.2zM256 159.1c-53.08 0-96 42.92-96 96c0 53.08 42.92 95.1 96 95.1s96-42.92 96-95.1C352 202.9 309.1 159.1 256 159.1zM256 280C242.8 280 232 269.3 232 256S242.8 232 256 232S280 242.8 280 256S269.3 280 256 280zM256 0C114.6 0 0 114.6 0 256s114.6 256 256 256s256-114.6 256-256S397.4 0 256 0zM256 464c-114.7 0-208-93.31-208-208S141.3 48 256 48s208 93.31 208 208S370.7 464 256 464z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(tvShowsViewModel)
      {
        IconPathData = "M432 96h-110l55.03-55.03c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0l-87.05 86.95L169 7.031c-9.375-9.375-24.56-9.375-33.94 0s-9.375 24.56 0 33.94L190.1 96H80C35.82 96 0 131.8 0 176v256C0 476.2 35.82 512 80 512h352c44.18 0 80-35.82 80-80v-256C512 131.8 476.2 96 432 96zM464 440c0 13.25-10.75 24-24 24H72c-13.25 0-24-10.75-24-24v-272c0-13.25 10.75-24 24-24h368c13.25 0 24 10.75 24 24V440zM416 200c-13.25 0-24 10.74-24 24c0 13.25 10.75 24 24 24S440 237.3 440 224C440 210.7 429.3 200 416 200zM300.8 192H147.2C118.9 192 96 214.3 96 241.8v124.4C96 393.7 118.9 416 147.2 416h153.6C329.1 416 352 393.7 352 366.2V241.8C352 214.3 329.1 192 300.8 192zM416 280c-13.25 0-24 10.74-24 24c0 13.25 10.75 24 24 24s24-10.75 24-24C440 290.7 429.3 280 416 280z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(fileBrowser)
      {
        IconPathData = "M544 96h-140.1l-49.14-45.25C342.7 38.74 326.5 32 309.5 32H192C156.7 32 128 60.66 128 96v224c0 35.34 28.65 64 64 64h352c35.35 0 64-28.66 64-64V160C608 124.7 579.3 96 544 96zM560 320c0 8.824-7.178 16-16 16H192c-8.822 0-16-7.176-16-16V96c0-8.824 7.178-16 16-16h117.5c4.273 0 8.293 1.664 11.31 4.688L384 144h160c8.822 0 16 7.176 16 16V320zM488 480H152C85.83 480 32 426.2 32 360v-240C32 106.8 42.75 96 56 96S80 106.8 80 120v240c0 39.7 32.3 72 72 72h336c13.25 0 24 10.75 24 24S501.3 480 488 480z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(upnp)
      {
        IconPathData = "M121 112C107.9 111.2 96.58 121.8 96.02 135C95.47 148.3 105.8 159.4 119 159.1c106.4 4.469 196.6 94.66 201 201C320.6 373.9 331.2 384 343.1 384c.3438 0 .6719 0 1.016-.0313c13.25-.5313 23.53-11.72 22.98-24.97C362.5 228.3 251.7 117.5 121 112zM121.6 208.1c-13.11-.4375-24.66 9.125-25.52 22.38c-.8594 13.22 9.172 24.66 22.39 25.5c54.69 3.562 102.1 50.94 105.6 105.6C224.9 374.3 235.4 384 247.1 384c.5313 0 1.062-.0313 1.594-.0625c13.22-.8438 23.25-12.28 22.39-25.5C266.9 280.6 199.4 213.1 121.6 208.1zM128 320c-8.189 0-16.38 3.121-22.63 9.371c-12.5 12.5-12.5 32.76 0 45.26C111.6 380.9 119.8 384 128 384s16.38-3.121 22.63-9.371c12.5-12.5 12.5-32.76 0-45.26C144.4 323.1 136.2 320 128 320zM384 32H64C28.65 32 0 60.66 0 96v320c0 35.34 28.65 64 64 64h320c35.35 0 64-28.66 64-64V96C448 60.66 419.3 32 384 32zM400 416c0 8.82-7.178 16-16 16H64c-8.822 0-16-7.18-16-16V96c0-8.82 7.178-16 16-16h320c8.822 0 16 7.18 16 16V416z"
      });

      NavigationViewModel.Items.Add(new NavigationItem(iptv)
      {
        IconPathData = "M201.5 344.5l47.48-47.48c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L167.5 310.5L72.06 215C67.11 210.1 60.33 207.7 53.27 208.1C46.3 208.6 39.89 212.2 35.75 217.8C12.36 249.7 0 287.4 0 327.1C0 429.1 82.95 512 184.9 512c39.66 0 77.45-12.38 109.3-35.75c5.641-4.156 9.203-10.53 9.734-17.53c.5313-6.969-2.016-13.84-6.969-18.78L201.5 344.5zM184.9 464C109.4 464 48 402.6 48 327.1c0-19.66 4.109-38.72 12.02-56.22L241.1 452C223.6 459.9 204.6 464 184.9 464zM216 0C202.8 0 192 10.75 192 24S202.8 48 216 48c136.8 0 248 111.3 248 248c0 13.25 10.75 24 24 24S512 309.3 512 296C512 132.8 379.2 0 216 0zM216 104C202.8 104 192 114.8 192 128s10.75 24 24 24c79.41 0 144 64.59 144 144C360 309.3 370.8 320 384 320s24-10.75 24-24C408 190.1 321.9 104 216 104z"
      });

      NavigationViewModel.Items.First().IsActive = true;

    }

    #endregion

    #region Filter

    public void Filter(string phrase)
    {
      var acutal = NavigationViewModel.Items.SingleOrDefault(x => x.IsActive);

      if (acutal != null && acutal.Model is IFilterable filterable)
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