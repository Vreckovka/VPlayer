﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KeyListener;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;

namespace VPlayer.Player.ViewModels
{
  public class PlayerViewModel : RegionCollectionViewModel
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private int actualSongIndex = 0;

    #endregion

    #region Constructors

    public PlayerViewModel(IRegionProvider regionProvider, [NotNull] IEventAggregator eventAggregator, IKernel kernel) : base(regionProvider)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      Kernel = kernel;
      RegistredViews.Add(typeof(PlayerView), new Tuple<string, bool>(RegionNames.PlayerRegion, false));
      RegistredViews.Add(typeof(WindowsPlayerView), new Tuple<string, bool>(RegionNames.WindowsPlayerContentRegion, false));
    }

    #endregion

    #region Properties

    public IKernel Kernel { get; set; }
    public override Dictionary<Type, Tuple<string, bool>> RegistredViews { get; set; } = new Dictionary<Type, Tuple<string, bool>>();
    public List<SongInPlayList> PlayList { get; set; }
    public VlcMediaPlayer MediaPlayer { get; private set; }
    public SongInPlayList ActualSong { get; private set; }
    public bool IsPlaying { get; set; }

    #endregion

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

    #endregion

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

    public void OnNextSong(SongInPlayList songInPlayList)
    {
      PlayNext(songInPlayList);
    }

    #endregion


    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

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
      MediaPlayer.TimeChanged += (sender, e) => { ActualSong.ActualPosition = ((VlcMediaPlayer)sender).Position; };

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
      eventAggregator.GetEvent<PlaySongsInPlayListEvent>().Subscribe(PlaySongInPlayList);
    }

    private void PlaySongInPlayList(SongInPlayList songInPlayList)
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

    #region PlaySongs

    private void PlaySongs(IEnumerable<SongInPlayList> songs)
    {
      actualSongIndex = 0;
      PlayList = songs.ToList();
      Play();

      if (ActualSong != null) ActualSong.IsPlaying = false;
    }

    #endregion

    #endregion

    #region Play

    public void Play()
    {
      Task.Run(() =>
      {
        if (PlayList.Count > 0)
        {
          if (ActualSong != null) ActualSong.IsPlaying = false;

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

    #endregion

    #region Pause

    public void Pause()
    {
      MediaPlayer.Pause();
    }

    #endregion

    #region PlayNext

    public void PlayNext(SongInPlayList nextSong = null)
    {
      if (nextSong == null)
        actualSongIndex++;
      else
        actualSongIndex = PlayList.FindIndex(x => x.Model.Id == nextSong.Model.Id);
      Play();
    }

    #endregion

    #region KeyListener_OnKeyPressed

    private void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
    {
      if (e.KeyPressed == Key.MediaPlayPause)
      {
        if (ActualSong.IsPlaying)
        {
          ActualSong.IsPlaying = false;
          Play();
        }
        else
        {
          ActualSong.IsPlaying = true;
          Pause();
        }
      }
      else if (e.KeyPressed == Key.MediaNextTrack)
      {
        PlayNext();
      }
    }

    #endregion

    #endregion
  }
}
