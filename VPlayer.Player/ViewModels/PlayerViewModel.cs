using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Listener;
using VCore;
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

      windowsPlayerViewModel.IsPlayingSubject.Subscribe(x =>
      {
        IsPlaying = x;
      });
    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.PlayerRegion;
    public IPlayableRegionViewModel ActualViewModel => windowsPlayerViewModel.IsActive ? (IPlayableRegionViewModel)windowsPlayerViewModel : webPlayerViewModel.IsActive ? webPlayerViewModel : null;
    public bool IsPlaying { get; set; }

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

    public void Play()
    {
      ActualViewModel?.Play();
    }

    public void Pause()
    {
      ActualViewModel?.Pause();
    }

    public void PlayNext()
    {
      ActualViewModel?.PlayNext();
    }

    #region KeyListener_OnKeyPressed

    private void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
    {
        if (e.KeyPressed == Key.MediaPlayPause)
        {
          if (!IsPlaying)
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

    #endregion KeyListener_OnKeyPressed

  }
}
