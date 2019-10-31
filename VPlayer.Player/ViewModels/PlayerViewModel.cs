using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Listener;
using VCore;
using VCore.ExtentionsMethods;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views;
using VPlayer.WebPlayer.ViewModels;

namespace VPlayer.Player.ViewModels
{
  public class PlayerViewModel : RegionViewModel<PlayerView>
  {
    private readonly WindowsPlayerViewModel windowsPlayerViewModel;
    private readonly WebPlayerViewModel webPlayerViewModel;
    private readonly KeyListener keyListener;

    public PlayerViewModel(
      IRegionProvider regionProvider,
      WindowsPlayerViewModel windowsPlayerViewModel,
      WebPlayerViewModel webPlayerViewModel,
      KeyListener keyListener) : base(regionProvider)
    {
      this.windowsPlayerViewModel = windowsPlayerViewModel ?? throw new ArgumentNullException(nameof(windowsPlayerViewModel));
      this.webPlayerViewModel = webPlayerViewModel ?? throw new ArgumentNullException(nameof(webPlayerViewModel));
      this.keyListener = keyListener ?? throw new ArgumentNullException(nameof(keyListener));

      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;


      windowsPlayerViewModel.ObservePropertyChange(x => x.CanPlay)
        .Subscribe((x) =>
        {
          CanPlay = x;
        });

      windowsPlayerViewModel.ObservePropertyChange(x => x.IsPlaying)
        .Subscribe((x) =>
        {
          IsPlaying = x;
        });

      windowsPlayerViewModel.ObservePropertyChange(x => x.IsActive)
        .Subscribe((x) =>
        {
          ActualViewModel = windowsPlayerViewModel;
        });

      ActualViewModel = windowsPlayerViewModel;

    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.PlayerRegion;
    public IPlayableRegionViewModel ActualViewModel { get; set; }
    public bool IsPlaying { get; set; }
    public bool CanPlay { get; set; }


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

    public void Play()
    {
      ActualViewModel?.Play();
    }

    #endregion

    #region Pause

    public void Pause()
    {
      ActualViewModel?.Pause();
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
      switch (e.KeyPressed)
      {
        case Key.MediaPlayPause:
          {
            if (!IsPlaying)
            {
              Play();
            }
            else
            {
              Pause();
            }
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
      }
    }

    #endregion KeyListener_OnKeyPressed

  }
}
