using System;
using System.Windows.Input;
using Listener;
using Ninject;
using VCore;
using VCore.Annotations;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
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

    public PlayerViewModel(
      IRegionProvider regionProvider,
      [NotNull] IKernel kernel,
      KeyListener keyListener) : base(regionProvider)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      this.keyListener = keyListener ?? throw new ArgumentNullException(nameof(keyListener));
    }

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.PlayerRegion;
    public IPlayableRegionViewModel ActualViewModel { get; set; }
    public bool IsPlaying { get; set; }
    public bool CanPlay { get; set; }


    public override void Initialize()
    {
      base.Initialize();

      SubscribeToPlayers();
    }

    private void SubscribeToPlayers()
    {
      var allPlayers = kernel.GetAll<IPlayableRegionViewModel>();

      keyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      foreach (var player in allPlayers)
      {
        player.ObservePropertyChange(x => x.CanPlay).Subscribe(x =>
        {
          CanPlay = x;
        }).DisposeWith(this);


        player.ObservePropertyChange(x => x.IsPlaying).Subscribe((x) =>
          {
            IsPlaying = x;
          }).DisposeWith(this);

        player.ObservePropertyChange(x => x.IsActive).Subscribe(x =>
        {
          if (x)
            ActualViewModel = player;
        }).DisposeWith(this);
      }
    }

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
      ActualViewModel?.PlayPause();
    }

    #endregion

    #region PlayNext

    public void PlayNext()
    {
      ActualViewModel?.PlayNext(null);
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
