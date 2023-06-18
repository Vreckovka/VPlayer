using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Listener;
using Ninject;
using Prism.Events;
using SoundManagement;
using VCore;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Helpers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.WindowsPlayer.Behaviors;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Player.ViewModels
{
  public class PlayerViewModel : RegionViewModel<PlayerView>
  {
    private readonly IKernel kernel;
    private readonly KeyListener keyListener;
    private readonly IStatusManager statusManager;
    private readonly IEventAggregator eventAggregator;
    private Window mainWindow;
    private IDisposable audioDeviceManagerDisposable;
    private SerialDisposable volumeDisposable;
    private IPlayableRegionViewModel[] players;

    public PlayerViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      KeyListener keyListener,
      IStatusManager statusManager,
      IEventAggregator eventAggregator,
      IPlayableRegionViewModel[] players) : base(regionProvider)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      this.keyListener = keyListener ?? throw new ArgumentNullException(nameof(keyListener));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      mainWindow = Application.Current.MainWindow;
      this.players = players;
    }

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.PlayerRegion;

    #region ActualViewModel

    private IPlayableRegionViewModel actualViewModel;

    public IPlayableRegionViewModel ActualViewModel
    {
      get { return actualViewModel; }
      set
      {
        if (value != actualViewModel)
        {
          actualViewModel = value;
          RaisePropertyChanged(nameof(ActualVolume));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanPlay

    private bool canPlay;

    public bool CanPlay
    {
      get { return canPlay; }
      set
      {
        if (value != canPlay)
        {
          canPlay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region StatusMessageViewModel

    public StatusMessageViewModel StatusMessageViewModel
    {
      get
      {
        return statusManager.ActualMessageViewModel;
      }
    }

    #endregion

    #region ActualVolume

    public int ActualVolume
    {
      get
      {
        if (ActualViewModel != null && ActualViewModel.Volume > 0)
        {
          return ActualViewModel.Volume;
        }
        else
        {
          return 100;
        }
      }
      set
      {
        if (value != ActualViewModel?.Volume)
        {
          bool mouseIsDown = Mouse.LeftButton == MouseButtonState.Pressed;

          if (!mouseIsDown)
            ActualViewModel?.SetVolumeAndRaiseNotification(value);
          else
          {
            ActualViewModel?.SetVolumeWihtoutNotification(value);
          }

          RaisePropertyChanged(nameof(ActualVolume));
        }
      }
    }

    #endregion

    #region IsDuoMode

    private bool isDuoMode;

    public bool IsDuoMode
    {
      get { return isDuoMode; }
      set
      {
        if (value != isDuoMode)
        {
          isDuoMode = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region NextCommand

    private ActionCommand nextCommand;

    public ICommand NextCommand
    {
      get
      {
        if (nextCommand == null)
        {
          nextCommand = new ActionCommand(PlayNext);
        }

        return nextCommand;
      }
    }

    #endregion

    #region PreviousCommand

    private ActionCommand previousCommand;

    public ICommand PreviousCommand
    {
      get
      {
        if (previousCommand == null)
        {
          previousCommand = new ActionCommand(PlayPrevious);
        }

        return previousCommand;
      }
    }

    #endregion

    #region Play

    private ActionCommand playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand(PlayPause);
        }

        return playButton;
      }
    }

    public void PlayPause()
    {
      ActualViewModel?.PlayPause();
    }

    #endregion

    #region ResetVolume

    private ActionCommand resetVolume;

    public ICommand ResetVolume
    {
      get
      {
        if (resetVolume == null)
        {
          resetVolume = new ActionCommand(OnResetVolume);
        }

        return resetVolume;
      }
    }

    public void OnResetVolume()
    {
      ActualVolume = 100;
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      volumeDisposable = new SerialDisposable().DisposeWith(this);

      SubscribeToPlayers();

      statusManager.OnStatusMessageUpdated.Subscribe(x =>
      {
        VSynchronizationContext.PostOnUIThread(() =>
        {
          RaisePropertyChanged(nameof(StatusMessageViewModel));
        });

      }).DisposeWith(this);


      eventAggregator.GetEvent<PlayPauseEvent>().Subscribe(PlayPause).DisposeWith(this);

      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(async x =>
      {
        await Task.Delay(500);

        RaisePropertyChanged(nameof(ActualVolume));

      }).DisposeWith(this);
    }

    #endregion

    #region SubscribeToPlayers

    private void SubscribeToPlayers()
    {
      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      foreach (var player in players)
      {
        if (player is VideoPlayerViewModel videoPlayerViewModel)
        {
          videoPlayerViewModel.PlayerViewModel = this;
        }

        if (player is MusicPlayerViewModel)
        {
          ChangeActualViewModel(player);
        }


        player.ObservePropertyChange(x => x.CanPlay).Subscribe(playerCanPlay =>
        {
          if (playerCanPlay && player.IsActive && player != ActualViewModel)
          {
            ChangeActualViewModel(player);
          }
          else if (ActualViewModel == player)
          {
            CanPlay = playerCanPlay;
          }
        }).DisposeWith(this);


        player.ObservePropertyChange(x => x.IsPlaying).Subscribe((x) =>
          {
            if (!IsDuoMode)
            {
              if (x)
              {
                foreach (var player1 in players)
                {
                  if (ActualViewModel != player1)
                  {
                    player1.Pause();
                    player1.IsSelectedToPlay = false;
                  }
                }
              }
            }

            IsPlaying = x;

          }).DisposeWith(this);

        player.ObservePropertyChange(x => x.IsActive).Subscribe(x =>
        {
          if (x && player.CanPlay && player != ActualViewModel)
          {
            ChangeActualViewModel(player);
          }

          if (player == ActualViewModel && originalActualViewModel != null)
          {
            if (!x && originalActualViewModel.IsPlaying && !ActualViewModel.IsPlaying)
            {
              ChangeActualViewModel(originalActualViewModel);
              originalActualViewModel = null;
            }
          }


        }).DisposeWith(this);
      }
    }

    #endregion

    #region ChangeActualViewModel

    private IPlayableRegionViewModel originalActualViewModel = null;
    private void ChangeActualViewModel(IPlayableRegionViewModel newPlayer)
    {
      volumeDisposable.Disposable?.Dispose();

      volumeDisposable.Disposable = newPlayer.OnVolumeChanged.Subscribe((x) => RaisePropertyChanged(nameof(ActualVolume)));

      if (ActualViewModel != null)
      {
        if (ActualViewModel.IsPlaying && !newPlayer.IsPlaying)
        {
          originalActualViewModel = ActualViewModel;
        }
        else if (!ActualViewModel.IsPlaying)
        {
          ActualViewModel.IsSelectedToPlay = false;
        }
      }

      ActualViewModel = newPlayer;

      if (ActualViewModel != null)
      {
        ActualViewModel.IsSelectedToPlay = true;
        IsPlaying = ActualViewModel.IsPlaying;
        CanPlay = ActualViewModel.CanPlay;
      }
    }

    #endregion

    #region PlayNext

    public void PlayNext()
    {
      ActualViewModel?.PlayNext();
    }

    #endregion

    #region PlayPrevious

    public void PlayPrevious()
    {
      ActualViewModel?.PlayPrevious();
    }

    #endregion

    #region KeyListener_OnKeyPressed

    private void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        var filePlayable = CanUseKey(e.KeyPressed);

        if (filePlayable != null)
        {
          switch (e.KeyPressed)
          {
            case Key.MediaPlayPause:
              {
                ActualViewModel?.PlayPause();
                break;
              }

            case Key.MediaNextTrack:
              {
                PlayNext();
                break;
              }

            case Key.MediaPreviousTrack:
              {
                PlayPrevious();
                break;
              }
            case Key.Left:
              {
                filePlayable?.SeekBackward();
                break;
              }
            case Key.Right:
              {
                filePlayable?.SeekForward();
                break;
              }
            case Key.Space:
              {
                filePlayable?.PlayPause();
                break;
              }
          }
        }
      });
    }

    #endregion KeyListener_OnKeyPressed

    #region CanUseKey

    private IFilePlayableRegionViewModel CanUseKey(Key key)
    {
      if (ActualViewModel != null && ActualViewModel is IFilePlayableRegionViewModel filePlayable1)
      {
        if (ActualViewModel.IsActive &&
            mainWindow.IsActive &&
            mainWindow.WindowState != WindowState.Minimized &&
            !VFocusManager.IsAnyFocused())
        {
          return filePlayable1;
        }
        else if (key == Key.MediaPlayPause ||
                 key == Key.MediaNextTrack ||
                 key == Key.MediaPreviousTrack)
        {
          return filePlayable1;
        }
      }


      return null;
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      audioDeviceManagerDisposable?.Dispose();

      keyListener.OnKeyPressed -= KeyListener_OnKeyPressed;
    }

    #endregion

    #endregion
  }
}
