using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Listener;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
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
            ActualVolume = (int)actualViewModel.Volume;
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

    #region StatusMessage

    public StatusMessage StatusMessage
    {
      get
      {
        return statusManager.ActualMessage;
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
          RaisePropertyChanged(nameof(StatusMessage));
        });

      }).DisposeWith(this);



      eventAggregator.GetEvent<PlayPauseEvent>().Subscribe(PlayPause).DisposeWith(this);
    }

    #endregion

    #region SubscribeToPlayers

    private SerialDisposable actualItemSerialDisposable = new SerialDisposable();
    private void SubscribeToPlayers()
    {
      var allPlayers = kernel.GetAll<IPlayableRegionViewModel>();

      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      foreach (var player in allPlayers)
      {
        if (player is VideoPlayerViewModel videoPlayerViewModel)
        {
          videoPlayerViewModel.PlayerViewModel = this;
        }


        player.ObservePropertyChange(x => x.CanPlay).Subscribe(x =>
        {
          if (x && player.IsActive)
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
                }
              }
            }

            IsPlaying = x;

          }).DisposeWith(this);

        player.ObservePropertyChange(x => x.IsActive).Subscribe(x =>
        {
          if (x && player.CanPlay)
          {
            ChangeActualViewModel(player);
          }
        }).DisposeWith(this);
      }
    }

    #endregion

    private void ChangeActualViewModel(IPlayableRegionViewModel newPlayer)
    {
      if (ActualViewModel != null)
      {
        ActualViewModel.IsSelectedToPlay = false;
      }

      ActualViewModel = newPlayer;

      ActualViewModel.IsSelectedToPlay = true;

      IsPlaying = ActualViewModel.IsPlaying;

      actualItemSerialDisposable.Disposable?.Dispose();


      CanPlay = ActualViewModel.CanPlay;
    }

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
            if (mainWindow.IsActive && ActualViewModel is IFilePlayableRegionViewModel filePlayable)
            {
              filePlayable?.SeekBackward();
            }
            break;
          }
        case Key.Right:
          {
            if (mainWindow.IsActive && ActualViewModel is IFilePlayableRegionViewModel filePlayable)
            {
              filePlayable?.SeekForward();
            }
            break;
          }
      }
    }

    #endregion KeyListener_OnKeyPressed

    #endregion
  }
}
