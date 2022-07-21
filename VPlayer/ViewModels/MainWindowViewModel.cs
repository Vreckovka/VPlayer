using System;
using System.Reflection;
using System.Text.RegularExpressions;
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
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Behaviors;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.AudioStorage.Scrappers.CSFD;
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
  //TODO: Reorder na playliste
  //TODO: Moznost editovat LRC
  //TODO: Mozno vyhladat titulky cez search a vybrat si
  //TODO: Hviezdicky pocet 
  //TODO: TV Channels editor dotiahnut
  //TODO: Save UPnP Sever (mozno je, bojim sa skusit)
  //
  // *****File Browser*****
  //TODO: File Browser - File watcher nad aktualnou zlozkou
  //TODO: File Browser - Prepinanie diskov (C,D,E...)
  //TODO: File Browser - zobrazuje slideshow obrazkov z videa(FFMPEG)
  //TODO: File Browser - Mark videne (ako dlho som to pozeral - % pozrete, kludne z celej zloky), ak som to pozeral aspon 5min a datum
  //
  //TODO: Pridavanie do playlistu vyber do akeho, alebo ci aktualne prehravajuceho, alebo chces vytvorit novy
  //TODO: Dotiahnut sezony zvlast ako update z csfd
  //TODO: Vymazat sezonu z tvshow
  //TODO: Play UPnP v prehravaci nejako zmenit (je tam iba daky dropdown) (vymysliet a vyrobit, nemusi byt nic zlozite)
  //TODO: Random a Opakovanie moznost v tv show playliste na prehravaci chyba
  //TODO: Listview pre Artistov, Albumy atd ... selection a potom akcie (vyberiem si 10 a dam spojit do seba, aristov rovnakych, ale nejako ich nespojilo)
  //TODO: Moznost presunu zlozku a subory do Serialy napr, alebo rovno do nejakeho serialu (Mozno totalne daleko to potiahnut ze moznost vytvorit rovno aj noveho)
  //TODO: Highlight videa hover nad itemom v playliste

  //TODO: Tv show card (kludne aj artist) - pustit od posledneho playlistu (zacne to tam kde si skoncil)
  //TODO: Last time played, dat ktory den (25.7.2019 (Pondelok))
  //
  // *****Playlist sidebar*****
  //        TODO  Playlist sidebar - aj vo fullscreene(why the fuck not) - schovany a rovnake mechanismus na zobrazenie
  //
  //  *****Easy*****
  //        TODO: Pridat thumbnail zo suboru
  //		    TODO: Ak Windows Volume je 100 a stale chcem viac tak dam VLC volume ++
  //		    TODO: Vymazat playlist + data iconka (iny trash button)
  //        TODO: Vymazat z databazy file item (Iconka do FILE WATCHERA) (oznacit ako stored a potom ked kliknes tak vymazes) nejaka db iconka alebo co
  //        
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
  //
  //
  //  *****PCloud******
  //       	TODO: Album cover ulozit do PCloudu
  //
  //
  //  	*****Webova db******
  //        TODO: Selector pre vyber databazy (Lokalna, Cloud)
  //        TODO: Playlisty ukladat podla typu suborov do prislusneho typu datbaty (lokal, cloud) 
  //
  //
  //  *****TOPKY*****
  //        TODO: Tv show prehrat od posledneho ulozene playlistu (to iste aj pre hudbu kludne), ked pribudne nova seria pusti to kde si skoncil, ale nacita ju
  //        TODO: Prehravanie,pridavanie do playlistu albumov, tv show z Detail viewu itemu (AlbumDetailView...) (tym padom mozno zalozka Albums nebude treba, aj tak tam nechodis, pojdes iba do artist)
  //        TODO: Vymazavat itemy z File browsera (ak je odkaz niekde do db na tu zlozku tak vymazat rovno aj tv show, ale spytat sa ci to chces a zapametat si ak ano)
  //        TODO: Mute button pri zvuku
  //        TODO: Umoznit simultalne spustit video a hudbu
  //        TODO: Umoznit zmenit cislo epizody a seriu
  //        TODO: Premenovat epizodu
  //        TODO: Ked pojdes hover nad playlist itemom tak sa zobrazi taka karta, kde budu detailne info + velka fotka
  //        TODO: Vytvorit HOME TAB (HOME -> ANALYTICS)  - Horizontal listview poslednych 5 - 10 playlistov a moznost ich hned spustit a potom rozne statistiky... (grafy, tabulky...) 
  //        TODO: Paging na dotahovanie playlistov (aj ostatnych veci) (aby sa neloadovala tak dlho appka a aj tak tie dole ma nezaujimaju)
  //        TODO: Zmena suborov (Napriklad ked nema Artista a ja to najdem tak ho nastavit) ---> Mozno cim skor a potom dat do PCloudu
  //
  //*********************************************************************************************************************************
  //
  //*****BUGS*****
  //TODO: Ked je buffering v prehravaci a das vypnut appku tak spadne a nevypne sa poriadne (zostane aj niekedy bezat potom na pozadi, nejaky thread niekde asi)
  //TODO: Nespojilo playlisty s rovnakym hash po spusteni (neviem ci TV show alebo hudba) (mozno tv show ze pustis z detailu a das save a potom znovu z detailu)
  //TODO: Nespaja niektorych aristov pri load
  //TODO: Sem tam ostanie vysiet appka
  //TODO: Po roztiahnuti okna spadlo
  //TODO: Rap mixy maju nejaky problem pri loading artistov
  //
  //  *****IMPORTANT!*****
  //        TODO: Po pridani novej serii nefunguje csfd download
  //        TODO: Pridanie artista cez Load music neskontroluje ci to bolo spustane z filebrowsera (indentificator ci je jednoznacny), treba skusit vyhladat existujuci item 
  //       
  //
  //********************************************************************************************************************************
  //
  //*****LONG RUN*****
  //TODO: Streaming pre subory, lyrics, tv shows (asi nie (mozno zopar)), kludne uplne vsetko (tv sources...).
  //TODO: Db hosting
  //TODO: WEB???? (Budem mat data)

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly ILogger logger;
    private readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      ILogger logger,
      ICSFDWebsiteScrapper iCsfdWebsiteScrapper) : base(viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.iCsfdWebsiteScrapper = iCsfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(iCsfdWebsiteScrapper));
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
      FullScreenManager.IsFullscreen = false;
      Window.Width = Window.RestoreBounds.Width;
      Window.Height = Window.RestoreBounds.Height;

      SwitchBehaviorCommand?.Execute(null);
    }

    #endregion

    #endregion

    #region Methods

    #region Initilize

    public override async void Initialize()
    {
      base.Initialize();

      AudioDeviceManager.Instance.RefreshAudioDevices();

#if DEBUG
      //Task.Run(() => iCsfdWebsiteScrapper.LoadTvShow("https://www.csfd.cz/film/742448-loki/recenze/"));
#endif

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();

      NavigationViewModel.Items.Add(new NavigationItem(windowsPlayer));

      var player = viewModelsFactory.Create<PlayerViewModel>();

      player.IsActive = true;
      windowsPlayer.IsActive = true;

    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      //base.Dispose();

      //foreach (var item in NavigationViewModel.Items)
      //{
      //  item?.Dispose();
      //}
    }

    #endregion

    #endregion
  }
}

