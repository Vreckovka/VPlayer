using System;
using System.Windows;
using System.Windows.Input;
using Listener;
using Ninject;
using VCore;
using VCore.Annotations;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Player.ViewModels
{
  public class PlayerViewModel : RegionViewModel<PlayerView>
  {
    private readonly IKernel kernel;
    private readonly KeyListener keyListener;
    private readonly IStatusManager statusManager;
    private Window mainWindow;

    public PlayerViewModel(
      IRegionProvider regionProvider,
      [NotNull] IKernel kernel,
      KeyListener keyListener,
      [NotNull] IStatusManager statusManager) : base(regionProvider)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      this.keyListener = keyListener ?? throw new ArgumentNullException(nameof(keyListener));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

      mainWindow = Application.Current.MainWindow;
    }

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.PlayerRegion;
    public IPlayableRegionViewModel ActualViewModel { get; set; }

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
    }

    #endregion


    #region SubscribeToPlayers

    private void SubscribeToPlayers()
    {
      var allPlayers = kernel.GetAll<IPlayableRegionViewModel>();

      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      foreach (var player in allPlayers)
      {
        player.ObservePropertyChange(x => x.CanPlay).Subscribe(x =>
        {
          if (ActualViewModel == player)
          {
            CanPlay = x;
          }
        }).DisposeWith(this);


        player.ObservePropertyChange(x => x.IsPlaying).Subscribe((x) =>
          {
            if (ActualViewModel == player)
            {
              IsPlaying = x;

              if (IsPlaying)
                foreach (var player1 in allPlayers)
                {
                  if (ActualViewModel != player1)
                  {
                    player1.Pause();
                  }
                }
            }
          }).DisposeWith(this);

        player.ObservePropertyChange(x => x.IsActive).Subscribe(x =>
        {
          if (x)
          {
            ActualViewModel = player;

            IsPlaying = ActualViewModel.IsPlaying;
          }
        }).DisposeWith(this);
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
          playButton = new ActionCommand(OnPlayButton);
        }

        return playButton;
      }
    }

    public void OnPlayButton()
    {
      ActualViewModel?.PlayPause();
    }

    #endregion Play

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
