using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HtmlAgilityPack;
using Prism.Events;
using Prism.Regions;
using SoundManagement;
using VCore;
using VCore.Helpers;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.Behaviors;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.AudioStorage.Parsers;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.ViewModels;
using VPlayer.UPnP.ViewModels;
using VPlayer.WindowsPlayer.Behaviors;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{

  //****FEATURES*****
        //TODO: Ak je neidentifkovana skladba, pridanie interpreta zo zoznamu, alebo vytvorit noveho
        //TODO: Nastavit si hlavnu zlozku a ked spustis z inej, moznost presunut
        //TODO: Playlist hore pri menu, quick ze prides a uvidis napriklad 5 poslednych hore v rade , ako carusel (5/5)
        //TODO: Playlist nech sa automaticky nevytvara ak je niekolko pesniciek (nastavenie pre uzivatela aky pocet sa ma ukladat!) (3/5)
        //TODO: Hore prenutie medzi windows a browser playermi , zmizne bocne menu
        //TODO: Pridat loading indikator, mozno aj co prave robi
        //TODO: Popupwindow is TOPMOST
        //TODO: Reorder na playliste
        //TODO: Upload lyrics do Google Drive
        //TODO: Moznost editovat lyrics
        //TODO: Mozno vyhladat titulky cez search a vybrat si
        //TODO: Hviezdicky pocet 
        //TODO: Obrazky do listview pri playlistoch (tv show cover atd...)
        //TODO: TV Channels editor dotiahnut
        //TODO: Save UPnP Sever (mozno je, bojim sa skusit)
        //TODO: File Browser - File watcher nad aktualnou zlozkou
        //TODO: File Browser - Podpora menit cestu
        //TODO: Popup menu style
        //TODO: Setting page - Poriadne settingy (ako default cesty do subor napr)
        //TODO: Ked je synced lyrics a nie si na tabe a ide to dalej a vratis sa tak to scrolne na tu dolnu poziciu animacne (nech to spravi instantne zacne od tade)
        //TODO: Pridavanie do playlistu vyber do akeho, alebo ci aktualne prehravajuceho, alebo chces vytvorit novy
        //TODO: Dotiahnut sezony zvlast ako update z csfd
        //TODO: Vymazat sezonu z tvshow
        //
        //  ****DESING***** 
        //      //TODO: Menu rozdelit na sub menu = Playlists(Songs, Music, TvShows), Library(Albums, Interprets, TvShows), Other(Iptv, UPnP...)
        //      //TODO: Cykli ked prejdes cely play list tak ze si ho cely vypocujes (meni sa farba podla cyklu)
        //      //TODO: Listview TileView / listview prepinac
        //      //TODO: TileVIew nad listviewom ked je velky aby bolo iba max poloziek na riadok (asi nebude len tak, treba prerobit dizajn karticky, alebo to spravit na center ako container v boostrape)
        //      //TODO: Playlist listview zobrazit ktory item je posledny (Nazov a poradie v playliste)
        //
        //  ****HARD/LONG***** 
        //      //TODO: Dotiahnut data o albumoch, serialoch (a vyznacit ktore mam a ktore mi chybaju)
        //
        //  ****TOPKY*****
        //      //TODO: Tv show prehrat od posledneho ulozene playlistu (to iste aj pre hudbu kludne), ked pribudne nova seria pusti to kde si skoncil, ale nacita ju
        //      //TODO: Vyber output device (zvukove vystupne zariadenie) aj vo fullscreene videa (why the fuck not), miesta do sirky je dost (nie do vysky)
        //      //TODO: Prehravanie,pridavanie do playlistu albumov, tv show z detailu (tym padom mozno zalozka Albums nebude treba, aj tak tam nechodis, pojdes iba do artist)
        //
        //
        //****LONG RUN*****
        //TODO: Streaming service, aby som nemusel mat db u seba na disku. Nejaky server niekde si kupit (Minio)
        //TODO: Streaming pre subory, lyrics, tv shows, kludne uplne vsetko (tv sources...). Db tam bude cele to pojde do webu  //
        //
        //
        //
        //****BUGS*****
        //TODO: Ked pripojim sluchatka cez bluethooth tak spadne appka (ale nie vzdy, pozeral som asi serial)
        //TODO: Nedava sa prec IsPlaying z itemu ked uz nie je v playliste (asi pri rerabke sa ta vetva vymazala)
        //TODO: Ked pises vo filtri a das medzernik tak to pauzne (ma ale chyta aj ked nema pocuvat)
        //TODO: totaly played time bezi a uklada sa aj ked prehravac pozasteveny
        //TODO: Ked je buffering v prehravaci a das vypnut appku tak spadne a nevypne sa poriadne (zostane aj niekedy bezat potom na pozadi, nejaky thread niekde asi)
        //
        //  ****IMPORTANT!****
                //TODO: Asi v prehravaci sa ukazuje zly cas totaly played time (v listviewe tam je 1Den a tam ukazuje 0) (ASI TO RESETLO!!!)



  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly IRegionManager regionManager;
    private readonly UPnPManagerViewModel uPnPManagerViewModel;

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IRegionManager regionManager,
      UPnPManagerViewModel uPnPManagerViewModel)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
      this.uPnPManagerViewModel = uPnPManagerViewModel ?? throw new ArgumentNullException(nameof(uPnPManagerViewModel));

      AudioDeviceManager.Instance.RefreshAudioDevices();

    }

    #endregion

    #region Properties

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    public override string Title => "VPlayer";

    #region IsFullScreenContentVisible

    private bool isFullScreenContentVisible;

    public bool IsFullScreenContentVisible
    {
      get { return isFullScreenContentVisible; }
      set
      {
        if (value != isFullScreenContentVisible)
        {
          isFullScreenContentVisible = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsWindows

    private bool isWindows = true;
    public bool IsWindows
    {
      get { return isWindows; }
      set
      {
        if (value != isWindows)
        {
          isWindows = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ICommand SwitchBehaviorCommand { get; set; }
   
    #endregion

    #region Commads

    #region PlayStop

    private ActionCommand playStop;

    public ICommand PlayStop
    {
      get
      {
        if (playStop == null)
        {
          playStop = new ActionCommand(OnPlayStop);
        }

        return playStop;
      }
    }

    public void OnPlayStop()
    {
      eventAggregator.GetEvent<PlayPauseEvent>().Publish();
    }

    #endregion

    #region SwitchScreenCommand

    private ActionCommand switchScreenCommand;

    public ICommand SwitchScreenCommand
    {
      get
      {
        return switchScreenCommand ??= new ActionCommand(SwitchScreen);
      }
    }


    private void SwitchScreen()
    {
      SwitchBehaviorCommand?.Execute(null);

      FullScreenManager.IsFullscreen = false;
    }

    #endregion

    #endregion

    #region Methods

    #region Initilize

    public override void Initialize()
    {
      base.Initialize();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();

      windowsPlayer.IsActive = true;

      NavigationViewModel.Items.Add(new NavigationItem(windowsPlayer));

      var player = viewModelsFactory.Create<PlayerViewModel>();

      player.IsActive = true;
     
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      foreach (var item in NavigationViewModel.Items)
      {
        item?.Dispose();
      }
    }

    #endregion

    #endregion
  }
}

