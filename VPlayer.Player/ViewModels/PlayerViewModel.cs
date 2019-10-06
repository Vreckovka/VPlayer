using KeyListener;
using Ninject;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.ItemsCollections;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using Vlc.DotNet.Core;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.Player.Views.WindowsPlayer;

namespace VPlayer.Player.ViewModels
{
  public class PlayerViewModel : RegionCollectionViewModel
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private int actualSongIndex = 0;

    #endregion Fields

    #region Constructors

    public PlayerViewModel(
      IRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel) : base(
      regionProvider)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      Kernel = kernel;
      RegistredViews.Add(typeof(PlayerView), new Tuple<string, bool>(RegionNames.PlayerRegion, false));
      RegistredViews.Add(typeof(WindowsPlayerView), new Tuple<string, bool>(RegionNames.WindowsPlayerContentRegion, false));
    }

    #endregion Constructors

    #region Properties

    public SongInPlayList ActualSong { get; private set; }
    public bool IsPlaying { get; set; }
    public IKernel Kernel { get; set; }
    public VlcMediaPlayer MediaPlayer { get; private set; }
    public RxObservableCollection<SongInPlayList> PlayList { get; set; } = new RxObservableCollection<SongInPlayList>();

    public override Dictionary<Type, Tuple<string, bool>> RegistredViews { get; set; } =
      new Dictionary<Type, Tuple<string, bool>>();

    #endregion Properties

    #region Commands

    #region Play

    private ActionCommand playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand(OnPlayButton);
        }

        return playButton;
      }
    }

    public void OnPlayButton()
    {
      if (IsPlaying)
        Pause();
      else
        Play();
    }

    #endregion Play

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
      PlayNext(songInPlayList);
    }

    #endregion NextSong

    #endregion Commands

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      PlayList.ItemRemoved.Subscribe(ItemsRemoved);

      KeyListener.KeyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      Views[typeof(WindowsPlayerView)].Header = "Player";

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

      MediaPlayer.EndReached += (sender, e) => { PlayNext(); };

      MediaPlayer.TimeChanged += (sender, e) => { ActualSong.ActualPosition = ((VlcMediaPlayer) sender).Position; };

      MediaPlayer.Paused += (sender, e) =>
      {
        ActualSong.IsPaused = true;
        IsPlaying = false;
      };

      MediaPlayer.Playing += (sender, e) =>
      {
        ActualSong.IsPlaying = true;
        ActualSong.IsPaused = false;
        IsPlaying = true;
      };

      eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlaySongs);
      eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Subscribe(PlaySongFromPlayList);
      eventAggregator.GetEvent<AddSongsEvent>().Subscribe(AddSongs);
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
        PlayNext(songInPlayList);
      }
    }

    #endregion PlaySongFromPlayList

    #region PlaySongs

    private void PlaySongs(IEnumerable<SongInPlayList> songs)
    {
      actualSongIndex = 0;
      PlayList.Clear();
      PlayList.AddRange(songs);
      Play();

      if (ActualSong != null) ActualSong.IsPlaying = false;
    }

    #endregion PlaySongs

    #region PlaySongs

    private void AddSongs(IEnumerable<SongInPlayList> songs)
    {
      PlayList.AddRange(songs);
    }

    #endregion PlaySongs

    #region Play

    public void Play()
    {
      Task.Run(() =>
      {
        if (PlayList.Count > 0)
        {
          if (ActualSong != null)
          {
            ActualSong.IsPlaying = false;
            ActualSong.IsPaused = false;
          }

          if (PlayList[actualSongIndex] == ActualSong)
          {
            MediaPlayer.Play();
          }
          else
          {
            ActualSong = PlayList[actualSongIndex];
            MediaPlayer.SetMedia(new Uri(PlayList[actualSongIndex].Model.DiskLocation));

            MediaPlayer.Play();
          }
        }
      });
    }

    #endregion Play

    #region Pause

    public void Pause()
    {
      MediaPlayer.Pause();
    }

    #endregion Pause

    #region PlayNext

    public void PlayNext(SongInPlayList nextSong = null)
    {
      if (nextSong == null)
      {
        actualSongIndex++;
        Play();
      }
      else
      {
        var item = PlayList.Single(x => x.Model.Id == nextSong.Model.Id);

        if (ActualSong != item)
        {
          actualSongIndex = PlayList.IndexOf(item);
          Play();
        }
      }
    }

    #endregion PlayNext

    #region KeyListener_OnKeyPressed

    private void KeyListener_OnKeyPressed(
      object sender,
      KeyPressedArgs e)
    {
      if (ActualSong != null)
      {
        if (e.KeyPressed == Key.MediaPlayPause)
        {
          if (ActualSong.IsPaused)
          {
            Play();
          }
          else
          {
            Pause();
          }
        }
        else if (e.KeyPressed == Key.MediaNextTrack)
        {
          PlayNext();
        }
      }
    }

    #endregion KeyListener_OnKeyPressed

    #endregion Methods
  }
}