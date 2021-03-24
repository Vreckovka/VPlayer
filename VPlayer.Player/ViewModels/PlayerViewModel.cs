using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Listener;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Annotations;
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
      [NotNull] IKernel kernel,
      KeyListener keyListener,
      [NotNull] IStatusManager statusManager,
      [NotNull] IEventAggregator eventAggregator) : base(regionProvider)
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

    #region IsFullScreen

    private bool isFullScreen;

    public bool IsFullScreen
    {
      get { return isFullScreen; }
      set
      {
        if (value != isFullScreen)
        {
          isFullScreen = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualItem

    private IItemInPlayList actuaĺItem;

    public IItemInPlayList ActualItem
    {
      get { return actuaĺItem; }
      set
      {
        if (value != actuaĺItem)
        {
          actuaĺItem = value;
          RaisePropertyChanged();
        }
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

    #endregion Play

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
          try
          {
            RaisePropertyChanged(nameof(StatusMessage));
          }
          catch (Exception ex)
          {

            throw;
          }
        });

      }).DisposeWith(this);

      ShowHideMouseManager.OnFullScreen.Subscribe((x) =>
      {
        Application.Current.Dispatcher.Invoke(() =>
          {
            IsFullScreen = x;
          });
      });

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

      if (ActualViewModel is MusicPlayerViewModel musicPlayer)
      {
        ActualItem = musicPlayer.ActualItem;

        if (ActualItem == null)
        {
          actualItemSerialDisposable.Disposable = musicPlayer.ActualItemChanged.Subscribe((x) => { ActualItem = musicPlayer.ActualItem; });
        }
      }
      else if (ActualViewModel is VideoPlayerViewModel videoPlayerViewModel)
      {
        ActualItem = videoPlayerViewModel.ActualItem;

        if (ActualItem == null)
        {
          actualItemSerialDisposable.Disposable = videoPlayerViewModel.ActualItemChanged.Subscribe((x) => { ActualItem = videoPlayerViewModel.ActualItem; });
        }
      }

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

    private int seekSize = 50;
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
            if (mainWindow.IsActive)
            {
              ActualViewModel.SeekBackward(seekSize);
            }
            break;
          }
        case Key.Right:
          {
            if (mainWindow.IsActive)
            {
              ActualViewModel.SeekForward(seekSize);
            }
            break;
          }
      }
    }

    #endregion KeyListener_OnKeyPressed

    #endregion
  }
}
