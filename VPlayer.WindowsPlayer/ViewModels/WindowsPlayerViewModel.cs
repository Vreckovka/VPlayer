using Listener;
using Ninject;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.ItemsCollections;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Core;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.Player.Views.WindowsPlayer;

//TODO: Cykli ked prejdes cely play list tak ze si ho cely vypocujes (meni sa farba podla cyklu)
//TODO: Hash playlistov, ked zavries appku tak ti vyhodi posledny playlist
//TODO: Vytvorenie playlistu na + , nezadavat menu ale da nazov interpreta a index playlistu ak ich je viac ako 1
//TODO: Nacitanie zo suboru
//TODO: Ak je neidentifkovana skladba, pridanie interpreta zo zoznamu, alebo vytvorit noveho
//TODO: Nastavit si hlavnu zlozku a ked spustis z inej, moznost presunut

namespace VPlayer.Player.ViewModels
{
  public class WindowsPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView>, INavigationItem
  {
    #region Fields

    private readonly IVPlayerRegionProvider regionProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private int actualSongIndex = 0;
    private Dictionary<SongInPlayList, bool> playBookInCycle = new Dictionary<SongInPlayList, bool>();
    private HashSet<SongInPlayList> shuffleList = new HashSet<SongInPlayList>();

    #endregion Fields

    #region Constructors

    public WindowsPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager) : base(regionProvider)
    {
      this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    #endregion Constructors

    #region Properties

    public SongInPlayList ActualSong { get; private set; }
    public override bool IsPlaying { get; protected set; }

    public override bool CanPlay
    {
      get { return PlayList.Count != 0; }
    }

    public VlcMediaPlayer MediaPlayer { get; private set; }
    public RxObservableCollection<SongInPlayList> PlayList { get; set; } = new RxObservableCollection<SongInPlayList>();
    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public string Header => "Player";
    public bool IsRepeate { get; set; }
    public bool IsShuffle { get; set; }
    public IKernel Kernel { get; set; }
    public int Cycle { get; set; }

    #endregion Properties

    #region Commands

    #region NextSong

    private ActionCommand<SongInPlayList> nextSong;

    public ICommand NextSong
    {
      get
      {
        if (nextSong == null)
        {
          nextSong = new ActionCommand<SongInPlayList>(OnNextSong);
        }

        return nextSong;
      }
    }

    public void OnNextSong(
      SongInPlayList songInPlayList)
    {
      PlayNextWithSong(songInPlayList);
    }

    #endregion 

    #region AlbumDetail

    private ActionCommand albumDetail;

    public ICommand AlbumDetail
    {
      get
      {
        if (albumDetail == null)
        {
          albumDetail = new ActionCommand(OnAlbumDetail);
        }

        return albumDetail;
      }
    }

    public void OnAlbumDetail()
    {
      regionProvider.ShowAlbumDetail(ActualSong.AlbumViewModel, RegionNames.WindowsPlayerContentRegion);
    }

    #endregion 

    #region SavePlaylist

    private ActionCommand savePlaylistCommand;

    public ICommand SavePlaylistCommandCommand
    {
      get
      {
        if (savePlaylistCommand == null)
        {
          savePlaylistCommand = new ActionCommand(OnSavePlaylist);
        }

        return savePlaylistCommand;
      }
    }

    public void OnSavePlaylist()
    {
      SavePlaylist();
    }

    #endregion 

    #endregion Commands

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      PlayList.ItemRemoved.Subscribe(ItemsRemoved);

      PlayList.CollectionChanged += PlayList_CollectionChanged;

      var currentAssembly = Assembly.GetEntryAssembly();
      var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
      var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

      var libDirectory = new DirectoryInfo(path.FullName);
      MediaPlayer = new VlcMediaPlayer(libDirectory);

      bool playFinished = false;

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        Console.Error.Write("An error occurred");
        playFinished = true;
      };

      MediaPlayer.EndReached += (sender, e) => { Task.Run(() => PlayNextWithSong()); };

      MediaPlayer.TimeChanged += (sender, e) => { ActualSong.ActualPosition = ((VlcMediaPlayer)sender).Position; };

      MediaPlayer.Paused += (sender, e) =>
      {
        ActualSong.IsPaused = true;
        IsPlaying = false;
        IsPlayingSubject.OnNext(IsPlaying);
      };

      MediaPlayer.Stopped += (sender, e) =>
      {
        ActualSong.IsPaused = false;
        ActualSong = null;
        actualSongIndex = 0;
        IsPlaying = false;
        IsPlayingSubject.OnNext(IsPlaying);
      };

      MediaPlayer.Playing += (sender, e) =>
      {
        ActualSong.IsPlaying = true;
        ActualSong.IsPaused = false;
        IsPlaying = true;
        IsPlayingSubject.OnNext(IsPlaying);
      };

      eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlaySongs);
      eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Subscribe(PlaySongFromPlayList);
    }


    #endregion Initialize

    #region ItemsRemoved

    private void ItemsRemoved(EventPattern<SongInPlayList> eventPattern)
    {
      eventPattern.EventArgs.ArtistViewModel.IsInPlaylist = false;
      eventPattern.EventArgs.AlbumViewModel.IsInPlaylist = false;
    }

    #endregion ItemsRemoved

    #region PlaySongFromPlayList

    private void PlaySongFromPlayList(SongInPlayList songInPlayList)
    {
      if (songInPlayList == ActualSong)
      {
        if (!ActualSong.IsPaused)
          Pause();
        else
          Play();
      }
      else
      {
        PlayNextWithSong(songInPlayList);
      }
    }

    #endregion PlaySongFromPlayList

    #region PlaySongs

    private void PlaySongs(PlaySongsEventData data)
    {
      switch (data.PlaySongsAction)
      {
        case PlaySongsAction.Play:
          PlaySongs(data.Songs);
          break;
        case PlaySongsAction.Add:
          PlayList.AddRange(data.Songs);

          if (ActualSong == null)
          {
            SetActualSong(0);
          }

          RaisePropertyChanged(nameof(CanPlay));
          SavePlaylist();
          break;
        case PlaySongsAction.PlayFromPlaylist:
          PlaySongs(data.Songs,false);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      RaisePropertyChanged(nameof(CanPlay));
    }

    private void PlaySongs(IEnumerable<SongInPlayList> songs, bool savePlaylist = true)
    {
      PlayList.Clear();
      PlayList.AddRange(songs);

      IsPlaying = true;
      PlayNext(0);

      if (ActualSong != null)
        ActualSong.IsPlaying = false;

      if (savePlaylist)
      {
        SavePlaylist();
      }
    }

    #endregion PlaySongs


    #region Play

    public override void Play()
    {
      if (ActualSong != null)
      {
        if (IsPlaying)
        {
          var media = MediaPlayer.GetMedia();
          if (media == null || media.NowPlaying != ActualSong.Model.DiskLocation)
          {//file:///D:/Hudba/2Pac Discography [2007]/--- Other albums ---/1998 - Greatest Hits/2Pac - 102 - 2 Of Amerikaz Most Wanted.mp3
            var location = new Uri(ActualSong.Model.DiskLocation);

            MediaPlayer.SetMedia(location);
          }
        }

        MediaPlayer.Play();

        CheckCycle();

      }
    }

    #endregion Play

    #region Pause

    public override void Pause()
    {
      MediaPlayer.Pause();
    }

    #endregion Pause

    #region PlayPrevious

    public override void PlayPrevious()
    {
      actualSongIndex--;

      if (actualSongIndex < 0)
      {
        actualSongIndex = 0;
      }

      PlayNext(actualSongIndex);
    }

    #endregion

    #region Stop

    public override void Stop()
    {
      MediaPlayer.Stop();
    }

    #endregion

    #region PlayNext

    public override void PlayNext(int? songIndex = null)
    {

      if (IsShuffle)
      {
        var random = new Random();
        var result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();

        actualSongIndex = random.Next(0, result.Count);
      }
      else if (songIndex == null)
      {
        actualSongIndex++;
      }
      else
      {
        actualSongIndex = songIndex.Value;
      }

      if (PlayList.Count > actualSongIndex)
      {
        if (ActualSong != null)
        {
          ActualSong.IsPlaying = false;
          ActualSong.IsPaused = false;
        }

        if (actualSongIndex == PlayList.Count)
        {
          if (IsRepeate)
            actualSongIndex = 0;
          else
          {
            Stop();
            return;
          }
        }

        SetActualSong(actualSongIndex);

        if (IsPlaying)
          Play();
        else
          ActualSong.IsPaused = true;
      }
    }

    public void PlayNextWithSong(SongInPlayList nextSong = null)
    {
      if (nextSong == null)
      {
        PlayNext();
      }
      else
      {
        var item = PlayList.Single(x => x.Model.Id == nextSong.Model.Id);

        if (ActualSong != item)
        {
          PlayNext(PlayList.IndexOf(item));
        }
      }
    }

    #endregion PlayNext

    #region SetActualSong

    private void SetActualSong(int index)
    {
      ActualSong = PlayList[index];
      ActualSong.IsPlaying = true;

      shuffleList.Add(ActualSong);
    }

    #endregion

    #region CheckCycle

    private void CheckCycle()
    {
      if (playBookInCycle.Count > 0 && playBookInCycle.All(x => x.Value))
      {
        Cycle++;

        foreach (var item in playBookInCycle)
        {
          playBookInCycle[item.Key] = false;
        }
      }
    }

    #endregion

    #region PlayList_CollectionChanged

    private void PlayList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:

          foreach (var item in e.NewItems)
          {
            playBookInCycle.Add((SongInPlayList)item, false);
          }

          break;
        case NotifyCollectionChangedAction.Remove:
          foreach (var item in e.OldItems)
          {
            playBookInCycle.Remove((SongInPlayList)item);
          }
          break;
        case NotifyCollectionChangedAction.Replace:

          foreach (var item in e.OldItems)
          {
            playBookInCycle.Remove((SongInPlayList)item);
          }

          foreach (var item in e.NewItems)
          {
            playBookInCycle.Add((SongInPlayList)item, false);
          }

          break;
        case NotifyCollectionChangedAction.Reset:
          playBookInCycle.Clear();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region SavePlaylist

    public void SavePlaylist()
    {
      var entityPlayList = new Playlist();
      var songs = new List<PlaylistSong>();

      for (int i = 0; i < PlayList.Count; i++)
      {
        var song = PlayList[i];

        songs.Add(new PlaylistSong()
        {
          IdSong = song.Model.Id,
          OrderInPlaylist = (i + 1)
        });
      }

      entityPlayList.PlaylistSongs = songs;
      var artits = PlayList.GroupBy(x => x.ArtistViewModel.Name);

      entityPlayList.Name = string.Join(", ", artits.Select(x => x.Key).ToArray()) + " " + DateTime.Now.ToShortDateString() ;


      storageManager.StoreData(entityPlayList);

    }

    #endregion

    #endregion Methods
  }
}