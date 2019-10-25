using Listener;
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
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Core;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.Player.Views.WindowsPlayer;

namespace VPlayer.Player.ViewModels
{
  public class WindowsPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView>, INavigationItem
  {
    #region Fields

    private readonly IVPlayerRegionProvider regionProvider;
    private readonly IEventAggregator eventAggregator;
    private int actualSongIndex = 0;

    #endregion Fields

    #region Constructors

    public WindowsPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel) : base(regionProvider)
    {
      this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    #endregion Constructors

    #region Properties

    public SongInPlayList ActualSong { get; private set; }
    public override bool IsPlaying { get; protected set; }
    public VlcMediaPlayer MediaPlayer { get; private set; }
    public RxObservableCollection<SongInPlayList> PlayList { get; set; } = new RxObservableCollection<SongInPlayList>();
    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public string Header => "Player";
    public bool IsRepeate { get; set; }
    public bool IsShuffle { get; set; }
    public IKernel Kernel { get; set; }

    #endregion Properties

    #region Commands

    public void OnPlayButton()
    {
      if (IsPlaying)
        Pause();
      else
        Play();
    }

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

    #endregion NextSong

    #endregion Commands

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      PlayList.ItemRemoved.Subscribe(ItemsRemoved);

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

    #region AddSongs

    private void AddSongs(IEnumerable<SongInPlayList> songs)
    {
      PlayList.AddRange(songs);
    }

    #endregion PlaySongs

    #region Play

    public override void Play()
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

    public override void Pause()
    {
      MediaPlayer.Pause();
    }

    #endregion Pause

    public override void PlayNext()
    {
      actualSongIndex++;
    }

    public override void PlayPrevious()
    {
      throw new NotImplementedException();
    }

    public override void Stop()
    {
      MediaPlayer.Stop();
    }

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


    #endregion Methods


  }
}