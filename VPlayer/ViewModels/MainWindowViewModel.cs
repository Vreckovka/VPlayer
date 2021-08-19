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
using Logger;
using Microsoft.EntityFrameworkCore;
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
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
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
  //*****FEATURES*****
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
  //TODO: TV Channels editor dotiahnut
  //TODO: Save UPnP Sever (mozno je, bojim sa skusit)
  //
  // *****File Browser*****
  //TODO: File Browser - File watcher nad aktualnou zlozkou
  //TODO: File Browser - Right click open folder
  //TODO: File Browser - Nastavit zlozku ako hlavnu
  //TODO: FileBrowser - zobrazuje slideshow obrazkov z videa(FFMPEG)
  //TODO: FileBrowser - Mark videne (ako dlho som to pozeral - % pozrete, kludne z celej zloky), ak som to pozeral aspon 5min a datum
  //
  //TODO: Setting page - Poriadne settingy (ako default cesty do subor napr)
  //TODO: Ked je synced lyrics a nie si na tabe a ide to dalej a vratis sa tak to scrolne na tu dolnu poziciu animacne (nech to spravi instantne zacne od tade)
  //TODO: Pridavanie do playlistu vyber do akeho, alebo ci aktualne prehravajuceho, alebo chces vytvorit novy
  //TODO: Dotiahnut sezony zvlast ako update z csfd
  //TODO: Vymazat sezonu z tvshow
  //TODO: Play UPnP v prehravaci nejako zmenit (je tam iba daky dropdown) (vymysliet a vyrobit, nemusi byt nic zlozite)
  //TODO: Random a Opakovanie moznost v tv show playliste na prehravaci chyba
  //TODO: Listview pre Artistov, Albumy atd ... selection a potom akcie (vyberiem si 10 a dam spojit do seba, aristov rovnakych, ale nejako ich nespojilo)
  //TODO: Moznost presunu zlozku a subory do Serialy napr, alebo rovno do nejakeho serialu (Mozno totalne daleko to potiahnut ze moznost vytvorit rovno aj noveho)
  //TODO: CSFD vyhladavanie podla nazvu suborov (nejaky vyhladavac, mozno ze sa ti zobrazi nazov suboru a vyselektujes vyhladavany vyraz (highlight) (nemusis pisat) a to sa vyberie do filtra)
  //TODO: Highlight videa hover nad itemom v playliste
  //TODO  Playlist sidebar aj vo fullscreene(why the fuck not) - schovany a rovnake mechanismus na zobrazenie
  //TODO: Tv show card (kludne aj artist) - pustit od posledneho playlistu (zacne to tam kde si skoncil)
  //TODO: Last time played, dat ktory den (25.7.2019 (Pondelok))
  //TODO: Windows Player - Moznost reloadnut subor (vlc co sa hra), iconka niekde a spusti sa to tam kde to skoncilo
  //
  // *****Playlist sidebar*****
  //TODO: Playlist sidebar - iconky do context menu
  //TODO: Playlist sidebar - horne info - iconcy miesto textu?
  //TODO: Playlist sidebar width na % z celkovej sirky(aby bol co najväcsi a stale pekne to bolo a video bolo velke)
  //
  //
  //  *****Easy*****
  //        TODO: Pridat thumbnail zo suboru
  //        TODO: Status popup close manual
  //        TODO: Moznost vymazat automaticky nahrane lyrics a zakazat stahovanie
  //
  //  *****DESING***** 
  //        TODO: Cykli ked prejdes cely play list tak ze si ho cely vypocujes (meni sa farba podla cyklu)
  //        TODO: Listview TileView / listview prepinac
  //        TODO: Obrazky do listview pri playlistoch (tv show cover atd...)
  //        TODO: TileVIew nad listviewom ked je velky aby bolo iba max poloziek na riadok (asi nebude len tak, treba prerobit dizajn karticky, alebo to spravit na center ako container v boostrape)
  //        TODO: Playlist listview zobrazit ktory item je posledny (Nazov a poradie v playliste)
  //        TODO: Carty Music Player, Video Player... tmava ak ma prazdny playlist
  //
  //  *****HARD/LONG***** 
  //        TODO: Dotiahnut data o albumoch, serialoch (a vyznacit ktore mam a ktore mi chybaju)
  //        TODO: Poriadny Search, nieco take ako ctrlt + T pre Resharpery, kludne aj na button a filtre po kategoriach (aby sa dalo pouzivat aj bez klavesnice)
  //        TODO: Prehravanie hudby z File browsera
  //
  //  *****TOPKY*****
  //        TODO: File Browser - Nahrat tv show rovno z tade
  //        TODO: Tv show prehrat od posledneho ulozene playlistu (to iste aj pre hudbu kludne), ked pribudne nova seria pusti to kde si skoncil, ale nacita ju
  //        TODO: Prehravanie,pridavanie do playlistu albumov, tv show z detailu (tym padom mozno zalozka Albums nebude treba, aj tak tam nechodis, pojdes iba do artist)
  //        TODO: Vymazavat itemy z File browsera (ak je odkaz niekde do db na tu zlozku tak vymazat rovno aj tv show, ale spytat sa ci to chces a zapametat si ak ano)
  //        TODO: Mute button pri zvuku
  //        TODO: Umoznit simultalne spustit video a hudbu
  //        TODO: Umoznit zmenit cislo epizody a seriu
  //        TODO: Premenovat epizodu
  //        TODO: Ked pojdes hover nad playlist itemom tak sa zobrazi taka karta, kde budu detailne info + velka fotka
  //        TODO: Vytvorit HOME TAB (HOME -> ANALYTICS)  - Horizontal listview poslednych 5 - 10 playlistov a moznost ich hned spustit a potom rozne statistiky... (grafy, tabulky...) 
  //
  //
  //*********************************************************************************************************************************
  //
  //*****BUGS*****
  //TODO: Nedava sa prec IsPlaying z itemu ked uz nie je v playliste (asi pri rerabke sa ta vetva vymazala)
  //TODO: Niekedy ked prepnes automaticky output sound device tak equalizer sa zastavi a da sa reloadnut ze pausnes a znovu spustis hudubu - mal som pusteny tv show a potom som z playlistu pustil hudbu
  //TODO: Ked je buffering v prehravaci a das vypnut appku tak spadne a nevypne sa poriadne (zostane aj niekedy bezat potom na pozadi, nejaky thread niekde asi)
  //TODO: Nespojilo playlisty s rovnakym hash po spusteni (neviem ci TV show alebo hudba) (mozno tv show ze pustis z detailu a das save a potom znovu z detailu)
  //TODO: Nespaja niektorych aristov pri load
  //TODO: Sem tam ostanie vysiet appka
  //TODO: VLC volume ako keby je 0
  //TODO: Po roztiahnuti okna spadlo
  //TODO: chromedriver sa nekopiruje pri publish
  //TODO: csfd download ked task zlyha tak Error popup hned mizne
  //
  //  *****IMPORTANT!*****
  //        TODO: Po pridani novej serii nefunguje csfd download
  //       
  //
  //********************************************************************************************************************************
  //
  //*****LONG RUN*****
  //TODO: Streaming service, aby som nemusel mat db u seba na disku. Nejaky server niekde si kupit (Minio)
  //TODO: Streaming pre subory, lyrics, tv shows, kludne uplne vsetko (tv sources...). Db tam bude cele to pojde do webu  //

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly ILogger logger;

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      ILogger logger)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    public override async void Initialize()
    {
      await TryMigrateDatabaseAsync();

      base.Initialize();

      AudioDeviceManager.Instance.RefreshAudioDevices();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();

      windowsPlayer.IsActive = true;

      NavigationViewModel.Items.Add(new NavigationItem(windowsPlayer));

      var player = viewModelsFactory.Create<PlayerViewModel>();

      player.IsActive = true;

    }

    #endregion

    private async Task TryMigrateDatabaseAsync()
    {
      logger.Log(MessageType.Inform, "Migrating database");

      var context = new AudioDatabaseContext();

      await context.Database.MigrateAsync();
    }

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

