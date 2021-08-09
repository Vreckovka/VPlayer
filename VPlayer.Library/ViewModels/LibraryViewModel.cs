using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.Interfaces;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Settings;
using VPlayer.IPTV;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.FileBrowser;
using VPlayer.Library.ViewModels.IPTV;
using VPlayer.Library.ViewModels.TvShows;
using VPlayer.Library.Views;
using VPlayer.UPnP.ViewModels;

namespace VPlayer.Library.ViewModels
{
  public class MenuViewModel : ViewModel, INavigationItem
  {
    public MenuViewModel(string header)
    {
      Header = header;
    }

    #region IsActive

    private bool isActive;

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string Header { get; } 
  }

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
      var settings = viewModelsFactory.Create<SettingsViewModel>();

      var vm = new MenuViewModel("Playlists");
      var vm1 = new MenuViewModel("Library");
      var vm2 = new MenuViewModel("Other");

      var playlistsMenuItem = new NavigationItem(vm)
      {
        IconPathData = "M512 32H64C28.65 32 0 60.65 0 96v320c0 35.35 28.65 64 64 64h448c35.35 0 64-28.65 64-64V96C576 60.65 547.3 32 512 32zM528 416c0 8.822-7.178 16-16 16H64c-8.822 0-16-7.178-16-16V96c0-8.822 7.178-16 16-16h448c8.822 0 16 7.178 16 16V416zM128 128C110.3 128 96 142.3 96 160c0 17.67 14.33 32 32 32s32-14.33 32-32C160 142.3 145.7 128 128 128zM128 224C110.3 224 96 238.3 96 256c0 17.67 14.33 32 32 32s32-14.33 32-32C160 238.3 145.7 224 128 224zM128 320c-17.67 0-32 14.33-32 32c0 17.67 14.33 32 32 32s32-14.33 32-32C160 334.3 145.7 320 128 320zM456 136H215.1C202.7 136 192 146.8 192 160S202.7 184 215.1 184H456c13.25 0 24-10.75 24-24S469.3 136 456 136zM456 232H215.1C202.7 232 192 242.8 192 256c0 13.25 10.75 24 23.1 24H456c13.25 0 24-10.75 24-24C480 242.7 469.3 232 456 232zM456 328H215.1C202.7 328 192 338.8 192 352s10.75 24 23.1 24H456c13.25 0 24-10.75 24-24S469.3 328 456 328z"
      };


      var songsPlaylistsMenuItem = new NavigationItem(songPlaylists)
      {
        IconPathData = "M24 95.97h240c13.25 0 24-10.75 24-24s-10.75-24-24-24h-240c-13.25 0-24 10.75-24 24S10.75 95.97 24 95.97zM498.8 6.162c-8.25-6.125-18.69-7.699-28.44-4.699l-112 35.38C345.1 41.09 336 53.34 336 67.34v299.1c-18.12-9.125-40.13-14.5-64-14.5c-61.88 0-112 35.88-112 80C160 476.1 210.1 512 272 512s112-35.88 112-80V195.4l105.6-33.38C503 157.7 512 145.5 512 131.5V31.96C512 21.71 507.1 12.16 498.8 6.162zM272 463.1c-39.75 0-64-20.75-64-31.1c0-11.25 24.25-32 64-32s64 20.75 64 32C336 443.2 311.8 463.1 272 463.1zM464 119.7L384 144.1V79.09l80-25.25V119.7zM24 223.1h240c13.25 0 24-10.75 24-24s-10.75-24-24-24h-240c-13.25 0-24 10.74-24 24S10.75 223.1 24 223.1zM136 303.1h-112c-13.25 0-24 10.75-24 24s10.75 24 24 24h112c13.25 0 24-10.75 24-24S149.3 303.1 136 303.1z"
      };

      playlistsMenuItem.SubItems.Add(songsPlaylistsMenuItem);


      var tvPlaylistsMenuItem = new NavigationItem(tvShowPlaylistsViewModel)
      {
        ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/tvshow-playlist.png"
      };

      playlistsMenuItem.SubItems.Add(tvPlaylistsMenuItem);


      var iptvPlaylistsMenuItem = new NavigationItem(iptvPlaylists)
      {
        IconPathData = "M201.5 344.5l47.48-47.48c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L167.5 310.5L72.06 215C67.11 210.1 60.33 207.7 53.27 208.1C46.3 208.6 39.89 212.2 35.75 217.8C12.36 249.7 0 287.4 0 327.1C0 429.1 82.95 512 184.9 512c39.66 0 77.45-12.38 109.3-35.75c5.641-4.156 9.203-10.53 9.734-17.53c.5313-6.969-2.016-13.84-6.969-18.78L201.5 344.5zM184.9 464C109.4 464 48 402.6 48 327.1c0-19.66 4.109-38.72 12.02-56.22L241.1 452C223.6 459.9 204.6 464 184.9 464zM216 0C202.8 0 192 10.75 192 24S202.8 48 216 48c136.8 0 248 111.3 248 248c0 13.25 10.75 24 24 24S512 309.3 512 296C512 132.8 379.2 0 216 0zM216 104C202.8 104 192 114.8 192 128s10.75 24 24 24c79.41 0 144 64.59 144 144C360 309.3 370.8 320 384 320s24-10.75 24-24C408 190.1 321.9 104 216 104z"
      };

      playlistsMenuItem.SubItems.Add(iptvPlaylistsMenuItem);






      var library = new NavigationItem(vm1)
      {
        IconPathData = "M600.2 32.97L592 35.03v368.1L320 463.6l-272-60.45V35.03L39.76 32.97C19.56 27.92 0 43.19 0 64.01v351.1c0 15 10.42 27.98 25.06 31.24l288 63.1c4.572 1.016 9.311 1.016 13.88 0l288-63.1C629.6 443.1 640 431 640 416V64.01C640 43.19 620.4 27.92 600.2 32.97zM97.97 375.2l216 56C315.9 431.8 317.1 432 320 432s4.062-.25 6.031-.7813l216-56C552.6 372.5 560 362.9 560 352V24c0-7.125-3.156-13.91-8.656-18.44c-5.5-4.562-12.66-6.469-19.72-5.156L320 39.59L108.4 .4062C101.3-.9062 94.16 1 88.66 5.562C83.16 10.09 80 16.88 80 24V352C80 362.9 87.41 372.5 97.97 375.2zM344 83.96L512 52.84v280.6l-168 43.56V83.96zM128 52.84l168 31.12v293L128 333.4V52.84z"
      };

      var artistsMenuItem = new NavigationItem(artistsViewModel)
      {
        IconPathData = "M224 256c70.69 0 128-57.31 128-128c0-70.69-57.31-128-128-128S96 57.31 96 128C96 198.7 153.3 256 224 256zM224 48c44.11 0 80 35.89 80 80c0 44.11-35.89 80-80 80S144 172.1 144 128C144 83.89 179.9 48 224 48zM323.6 464H48.99C56.89 400.9 110.8 352 176 352h96c27.91 0 53.46 9.367 74.51 24.59c11.15-11.85 25.33-21.48 41.8-28.44C357.3 320.8 316.6 304 272 304h-96C78.8 304 0 382.8 0 480c0 17.67 14.33 32 32 32h323.2C339.8 498.9 328.8 482.5 323.6 464zM601.7 160.6l-96 19.2C490.8 182.8 480 195.1 480 211.2v161.2C469.9 369.7 459.3 368 448 368c-53.02 0-96 32.23-96 72c0 39.76 42.98 72 96 72s96-32.24 96-72V300.2l70.28-14.05C629.2 283.1 640 269.1 640 254.7V192C640 171.8 621.5 156.7 601.7 160.6z"
      };

      library.SubItems.Add(artistsMenuItem);

      var albumsMenuItem = new NavigationItem(albumsViewModel)
      {
        IconPathData = "M227.5 131.2c8.609-1.953 14.03-10.52 12.08-19.14c-1.953-8.609-10.55-13.95-19.14-12.08c-59.7 13.5-107 60.81-120.5 120.5C97.1 229.1 103.4 237.7 112 239.6C113.2 239.9 114.4 240 115.6 240c7.312 0 13.91-5.047 15.59-12.47C141.1 179.8 179.8 141.1 227.5 131.2zM256 159.1c-53.08 0-96 42.92-96 96c0 53.08 42.92 95.1 96 95.1s96-42.92 96-95.1C352 202.9 309.1 159.1 256 159.1zM256 280C242.8 280 232 269.3 232 256S242.8 232 256 232S280 242.8 280 256S269.3 280 256 280zM256 0C114.6 0 0 114.6 0 256s114.6 256 256 256s256-114.6 256-256S397.4 0 256 0zM256 464c-114.7 0-208-93.31-208-208S141.3 48 256 48s208 93.31 208 208S370.7 464 256 464z"
      };

      library.SubItems.Add(albumsMenuItem);

      var tvShowsMenuItem = new NavigationItem(tvShowsViewModel)
      {
        IconPathData = "M432 96h-110l55.03-55.03c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0l-87.05 86.95L169 7.031c-9.375-9.375-24.56-9.375-33.94 0s-9.375 24.56 0 33.94L190.1 96H80C35.82 96 0 131.8 0 176v256C0 476.2 35.82 512 80 512h352c44.18 0 80-35.82 80-80v-256C512 131.8 476.2 96 432 96zM464 440c0 13.25-10.75 24-24 24H72c-13.25 0-24-10.75-24-24v-272c0-13.25 10.75-24 24-24h368c13.25 0 24 10.75 24 24V440zM416 200c-13.25 0-24 10.74-24 24c0 13.25 10.75 24 24 24S440 237.3 440 224C440 210.7 429.3 200 416 200zM300.8 192H147.2C118.9 192 96 214.3 96 241.8v124.4C96 393.7 118.9 416 147.2 416h153.6C329.1 416 352 393.7 352 366.2V241.8C352 214.3 329.1 192 300.8 192zM416 280c-13.25 0-24 10.74-24 24c0 13.25 10.75 24 24 24s24-10.75 24-24C440 290.7 429.3 280 416 280z"
      };

      library.SubItems.Add(tvShowsMenuItem);





      var otherMenuItem = new NavigationItem(vm2)
      {
        IconPathData = "M223.1 96.1l.0046 48.74L253.7 174.6c-6.878-34.87 3.493-70.37 28.49-95.37c20.25-20.25 47.12-31.13 74.99-31.13l1.125-.0001L300.1 105.2l15.13 90.61l90.62 15.11l57.24-57.25c.2526 28.25-10.62 55.49-31.24 75.99c-9.249 9.375-20.12 16.5-31.75 21.63c1.75 1.625 3.874 2.899 5.625 4.648l30.63 30.72c10.5-6.375 20.5-14 29.5-23c37.99-37.1 53.61-94.24 40.61-146.5c-3.001-12.25-12.38-21.87-24.38-25.24c-12.25-3.373-25.25 .1273-33.1 9.002l-58.74 58.62l-32.37-5.371l-5.378-32.5l58.62-58.5c8.874-8.1 12.25-21.1 8.871-33.1c-3.251-12.12-13-21.5-25.25-24.49c-53.12-13.24-107.9 2.01-146.5 40.51c-10.25 10.25-18.5 21.75-25.37 33.1l1.125 .7498L223.1 96.1zM106 453.9c-12.75 12.75-35.25 12.75-48.12 .0045c-6.375-6.374-10-14.1-10-23.1c-.0009-9.124 3.498-17.62 9.997-23.1L192.3 271.6L158.4 237.7l-134.4 134.2c-15.5 15.5-23.1 36.12-23.99 57.1s8.503 42.49 24 57.99c15.5 15.5 36.12 23.99 57.1 23.99c21.87-.002 42.5-8.503 57.99-24l100.9-100.9c-9.626-15.87-15.13-33.87-15.63-52.12L106 453.9zM501.1 395.6l-117.1-117c-23.13-23.12-57.65-27.65-85.39-13.9L191.1 158L191.1 95.99l-127.1-95.99L0 63.1l96 127.1l62.04 .0077l106.7 106.6c-13.62 27.75-9.218 62.27 13.91 85.39l117 117.1c14.5 14.62 38.21 14.62 52.71-.0016l52.75-52.75C515.6 433.7 515.6 410.1 501.1 395.6z"
      };

      var fileBrowserMenuItem = new NavigationItem(fileBrowser)
      {
        IconPathData = "M544 96h-140.1l-49.14-45.25C342.7 38.74 326.5 32 309.5 32H192C156.7 32 128 60.66 128 96v224c0 35.34 28.65 64 64 64h352c35.35 0 64-28.66 64-64V160C608 124.7 579.3 96 544 96zM560 320c0 8.824-7.178 16-16 16H192c-8.822 0-16-7.176-16-16V96c0-8.824 7.178-16 16-16h117.5c4.273 0 8.293 1.664 11.31 4.688L384 144h160c8.822 0 16 7.176 16 16V320zM488 480H152C85.83 480 32 426.2 32 360v-240C32 106.8 42.75 96 56 96S80 106.8 80 120v240c0 39.7 32.3 72 72 72h336c13.25 0 24 10.75 24 24S501.3 480 488 480z"
      };

      otherMenuItem.SubItems.Add(fileBrowserMenuItem);

      var upnpMenuItem = new NavigationItem(upnp)
      {
        IconPathData = "M121 112C107.9 111.2 96.58 121.8 96.02 135C95.47 148.3 105.8 159.4 119 159.1c106.4 4.469 196.6 94.66 201 201C320.6 373.9 331.2 384 343.1 384c.3438 0 .6719 0 1.016-.0313c13.25-.5313 23.53-11.72 22.98-24.97C362.5 228.3 251.7 117.5 121 112zM121.6 208.1c-13.11-.4375-24.66 9.125-25.52 22.38c-.8594 13.22 9.172 24.66 22.39 25.5c54.69 3.562 102.1 50.94 105.6 105.6C224.9 374.3 235.4 384 247.1 384c.5313 0 1.062-.0313 1.594-.0625c13.22-.8438 23.25-12.28 22.39-25.5C266.9 280.6 199.4 213.1 121.6 208.1zM128 320c-8.189 0-16.38 3.121-22.63 9.371c-12.5 12.5-12.5 32.76 0 45.26C111.6 380.9 119.8 384 128 384s16.38-3.121 22.63-9.371c12.5-12.5 12.5-32.76 0-45.26C144.4 323.1 136.2 320 128 320zM384 32H64C28.65 32 0 60.66 0 96v320c0 35.34 28.65 64 64 64h320c35.35 0 64-28.66 64-64V96C448 60.66 419.3 32 384 32zM400 416c0 8.82-7.178 16-16 16H64c-8.822 0-16-7.18-16-16V96c0-8.82 7.178-16 16-16h320c8.822 0 16 7.18 16 16V416z"
      };

      otherMenuItem.SubItems.Add(upnpMenuItem);

      var iptvManagerMenuItem = new NavigationItem(iptv)
      {
        IconPathData = "M201.5 344.5l47.48-47.48c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L167.5 310.5L72.06 215C67.11 210.1 60.33 207.7 53.27 208.1C46.3 208.6 39.89 212.2 35.75 217.8C12.36 249.7 0 287.4 0 327.1C0 429.1 82.95 512 184.9 512c39.66 0 77.45-12.38 109.3-35.75c5.641-4.156 9.203-10.53 9.734-17.53c.5313-6.969-2.016-13.84-6.969-18.78L201.5 344.5zM184.9 464C109.4 464 48 402.6 48 327.1c0-19.66 4.109-38.72 12.02-56.22L241.1 452C223.6 459.9 204.6 464 184.9 464zM216 0C202.8 0 192 10.75 192 24S202.8 48 216 48c136.8 0 248 111.3 248 248c0 13.25 10.75 24 24 24S512 309.3 512 296C512 132.8 379.2 0 216 0zM216 104C202.8 104 192 114.8 192 128s10.75 24 24 24c79.41 0 144 64.59 144 144C360 309.3 370.8 320 384 320s24-10.75 24-24C408 190.1 321.9 104 216 104z"
      };

      otherMenuItem.SubItems.Add(iptvManagerMenuItem);

      otherMenuItem.SubItems.Add(new NavigationItem(settings)
      {
        IconPathData = "M504.1 315.1c0-8.652-4.607-16.84-12.36-21.39l-32.91-18.97C459.5 269.1 459.8 262.5 459.8 256s-.3228-13.1-.9683-19.62l32.91-18.97c7.752-4.548 12.36-12.74 12.36-21.39c0-21.27-49.32-128.2-84.52-128.2c-4.244 0-8.51 1.094-12.37 3.357l-32.78 18.97c-10.71-7.742-22.07-14.32-34.07-19.74V32.49c0-11.23-7.484-21.04-18.33-23.88C300.5 2.871 278.3 0 256 0C233.8 0 211.5 2.871 189.9 8.613C179.1 11.45 171.6 21.26 171.6 32.49v37.94c-12 5.42-23.36 12-34.07 19.74l-32.78-18.97C100.9 68.94 96.63 67.85 92.38 67.85c-.0025 0 .0025 0 0 0c-32.46 0-84.52 101.7-84.52 128.2c0 8.652 4.607 16.84 12.36 21.39l32.91 18.97C52.49 242.9 52.17 249.5 52.17 256s.3228 13.1 .9683 19.62L20.23 294.6C12.47 299.1 7.867 307.3 7.867 315.1c0 21.27 49.32 128.2 84.52 128.2c4.244 0 8.51-1.094 12.37-3.357l32.78-18.97c10.71 7.742 22.07 14.32 34.07 19.74v37.94c0 11.23 7.484 21.04 18.33 23.88C211.5 509.1 233.7 512 255.1 512c22.25 0 44.47-2.871 66.08-8.613c10.84-2.84 18.33-12.65 18.33-23.88v-37.94c12-5.42 23.36-12 34.07-19.74l32.78 18.97c3.855 2.264 8.123 3.357 12.37 3.357C452.1 444.2 504.1 342.4 504.1 315.1zM415.2 389.1l-43.66-25.26c-42.06 30.39-32.33 24.73-79.17 45.89v50.24c-13.29 2.341-25.58 3.18-36.44 3.18c-15.42 0-27.95-1.693-36.36-3.176v-50.24c-46.95-21.21-37.18-15.54-79.17-45.89l-43.64 25.25c-15.74-18.69-28.07-40.05-36.41-63.11L103.1 301.7C101.4 276.1 100.1 266.1 100.1 256c0-10.02 1.268-20.08 3.81-45.76L60.37 185.2C68.69 162.1 81.05 140.7 96.77 122l43.66 25.26c42.06-30.39 32.33-24.73 79.17-45.89V51.18c13.29-2.341 25.58-3.18 36.44-3.18c15.42 0 27.95 1.693 36.36 3.176v50.24c46.95 21.21 37.18 15.54 79.17 45.89l43.64-25.25c15.74 18.69 28.07 40.05 36.4 63.11L408 210.3c2.538 25.64 3.81 35.64 3.81 45.68c0 10.02-1.268 20.08-3.81 45.76l43.58 25.12C443.3 349.9 430.9 371.3 415.2 389.1zM256 159.1c-52.88 0-96 43.13-96 96S203.1 351.1 256 351.1s96-43.13 96-96S308.9 159.1 256 159.1zM256 304C229.5 304 208 282.5 208 256S229.5 208 256 208s48 21.53 48 48S282.5 304 256 304z"
      });


      NavigationViewModel.Items.Add(playlistsMenuItem);
      NavigationViewModel.Items.Add(library);
      NavigationViewModel.Items.Add(otherMenuItem);

      NavigationViewModel.Items.First().SubItems.First().IsActive = true;

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