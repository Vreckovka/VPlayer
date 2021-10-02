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
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.WPF.Managers;
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


    public PlayerViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      KeyListener keyListener,
      IStatusManager statusManager,
      IEventAggregator eventAggregator) : base(regionProvider)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      this.keyListener = keyListener ?? throw new ArgumentNullException(nameof(keyListener));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      mainWindow = Application.Current.MainWindow;
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

          if (actualViewModel != null)
          {
            ActualVolume = (int)actualViewModel.MediaPlayer.Volume;
          }

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

    private bool camPlay;

    public bool CanPlay
    {
      get { return camPlay; }
      set
      {
        if (value != camPlay)
        {
          camPlay = value;
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

    private int actualVolume = 100;

    public int ActualVolume
    {
      get { return actualVolume; }
      set
      {
        if (value != actualVolume)
        {
          actualVolume = value;

          RaisePropertyChanged();

          ActualViewModel?.SetVolume(value);
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

      SubscribeToPlayers();

      statusManager.OnStatusMessageUpdated.Subscribe(x =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          RaisePropertyChanged(nameof(StatusMessageViewModel));
        });

      }).DisposeWith(this);


      eventAggregator.GetEvent<PlayPauseEvent>().Subscribe(PlayPause).DisposeWith(this);

      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(async x =>
      {
        await Task.Delay(500);

        ActualViewModel?.SetVolume(ActualVolume);

      }).DisposeWith(this);
    }

    #endregion

    #region SubscribeToPlayers

    private void SubscribeToPlayers()
    {
      var allPlayers = kernel.GetAll<IPlayableRegionViewModel>().ToList();

      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      foreach (var player in allPlayers)
      {
        if (player is VideoPlayerViewModel videoPlayerViewModel)
        {
          videoPlayerViewModel.PlayerViewModel = this;
        }


        player.ObservePropertyChange(x => x.CanPlay).Subscribe(x =>
        {
          if (x && player.IsActive && player != ActualViewModel)
          {
            ChangeActualViewModel(player);
          }
        }).DisposeWith(this);


        player.ObservePropertyChange(x => x.IsPlaying).Subscribe((x) =>
          {
            if (x)
            {
              foreach (var player1 in allPlayers)
              {
                if (ActualViewModel != player1)
                {
                  player1.Pause();
                  player1.IsSelectedToPlay = false;
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
      if (ActualViewModel != null)
      {
        if (ActualViewModel.IsPlaying && !newPlayer.IsPlaying)
        {
          originalActualViewModel = ActualViewModel;
        }
        else
        {
          ActualViewModel.IsSelectedToPlay = false;
        }
      }


      ActualViewModel = newPlayer;
      ActualViewModel.IsSelectedToPlay = true;
      IsPlaying = ActualViewModel.IsPlaying;
      CanPlay = ActualViewModel.CanPlay;
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
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        var filePlayable = CanUseKey();

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


    private IFilePlayableRegionViewModel CanUseKey()
    {
      return Application.Current.Dispatcher.Invoke(() =>
      {
        if (ActualViewModel != null && ActualViewModel.IsActive &&
            ActualViewModel is IFilePlayableRegionViewModel filePlayable1 &&
            mainWindow.IsActive &&
            mainWindow.WindowState != WindowState.Minimized &&
            !VFocusManager.IsAnyFocused())
        {
          return filePlayable1;
        }

        return null;
      });
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
