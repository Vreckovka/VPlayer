using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Logger;
using VCore.Standard;
using VPlayer.WindowsPlayer.Players;
using VVLC.Providers;

namespace VVLC.Players
{
  public class VLCPlayer : ViewModel, IPlayer
  {
    private readonly IVlcProvider vlcProvider;
    private readonly ILogger logger;
    private LibVLC libVLC;


    public VLCPlayer(IVlcProvider vlcProvider, ILogger logger)
    {
      this.vlcProvider = vlcProvider ?? throw new ArgumentNullException(nameof(vlcProvider));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    #region Volume

    public int Volume
    {
      get { return MediaPlayer?.Volume ?? 100; }
      set
      {
        if (MediaPlayer != null && value != MediaPlayer.Volume)
        {
          MediaPlayer.Volume = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlaying

    public bool IsPlaying
    {
      get { return MediaPlayer.IsPlaying; }
    }

    #endregion

    #region IsMuted

    public bool IsMuted
    {
      get
      {
        return MediaPlayer?.Mute ?? false;
      }
    }

    #endregion

    #region Position

    public float Position
    {
      get { return MediaPlayer.Position; }
      set
      {
        if (value != MediaPlayer.Position)
        {
          MediaPlayer.Position = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Length

    public long Length
    {
      get { return MediaPlayer.Length; }
    }

    #endregion


    public event EventHandler EncounteredError;
    public event EventHandler EndReached;
    public event EventHandler Paused;
    public event EventHandler Stopped;
    public event EventHandler Playing;
    public event EventHandler Muted;
    public event EventHandler Unmuted;
    public event EventHandler<PlayerBufferingEventArgs> Buffering;
    public event EventHandler<PlayerTimeChangedArgs> TimeChanged;

    public IMedia Media { get; set; }


    #region MediaPlayer

    private MediaPlayer mediaPlayer;

    public MediaPlayer MediaPlayer
    {
      get { return mediaPlayer; }
      set
      {
        if (value != mediaPlayer)
        {
          mediaPlayer = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Initilize

    public void Initilize()
    {
      libVLC = vlcProvider.InitlizeVlc();
      MediaPlayer = new MediaPlayer(libVLC);
#if DEBUG
      //libVLC.Log += LibVLC_Log;
#endif
      HookToVlcEvents();
    }

    private void LibVLC_Log(object sender, LogEventArgs e)
    {
      logger.Log(MessageType.Inform,$"{MediaPlayer.GetHashCode()} {e.FormattedLog}");
    }

    #endregion

    public void ToggleMute()
    {
      MediaPlayer?.ToggleMute();
    }


    #region HookToVlcEvents

    protected void HookToVlcEvents()
    {
      Volume = MediaPlayer.Volume;

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      MediaPlayer.NothingSpecial += (s, e) =>
      {
        logger.Log(Logger.MessageType.Warning, e.ToString(), true);
      };

      MediaPlayer.Corked += (s, e) =>
      {
        logger.Log(Logger.MessageType.Warning, e.ToString(), true);
      };

      MediaPlayer.Uncorked += (s, e) =>
      {
        logger.Log(Logger.MessageType.Warning, e.ToString(), true);
      };

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        OnEncounteredError();
      };


      MediaPlayer.EndReached += (sender, e) =>
      {
        //VLC is firing end reached lot sooner than it should
        Thread.Sleep(1500);

        OnEndReached();
      };

      MediaPlayer.Paused += (sender, e) =>
      {
        OnPaused();
      };

      MediaPlayer.Stopped += (sender, e) =>
      {
        OnStopped();
      };

      MediaPlayer.Playing += (sender, e) =>
      {
        OnPlaying();
      };

      MediaPlayer.TimeChanged += (sender, e) =>
      {
        OnTimeChanged(new PlayerTimeChangedArgs()
        {
          Time = e.Time
        });
      };

      MediaPlayer.Buffering += (sender, e) =>
      {
        OnBuffering(new PlayerBufferingEventArgs()
        {
          Cache = e.Cache
        });
      };

      MediaPlayer.Muted += (sender, e) =>
      {
        OnMuted();
      };

      MediaPlayer.Unmuted += (sender, e) =>
      {
        OnUnmuted();
      };
    }

    #endregion

    public void Play()
    {
      lock (this)
      {
        MediaPlayer?.Play();
      }
    }

    public void Pause()
    {
      lock (this)
      {
        MediaPlayer?.Pause();
      }
    }

    public void Stop()
    {
      lock (this)
      {
        MediaPlayer?.Stop();
      }
    }

    #region SetNewMedia

    public void SetNewMedia(Uri source, CancellationToken cancellationToken)
    {
      if (source != null)
      {
        var mediaOptions = new string[0];

        if (source.AbsoluteUri.Contains("http"))
        {
          mediaOptions = new string[]
            {
                ":sout-keep",
                ":http-caching"
            };
        }
      

        var media = new Media(libVLC, source, mediaOptions);

        Media = new VLCMedia(media);

        MediaPlayer.Media = media;
      }
      else
      {
        MediaPlayer.Media = null;
      }
    }

    #endregion

    #region Events

    protected virtual void OnEncounteredError()
    {
      EncounteredError?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnEndReached()
    {
      EndReached?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPaused()
    {
      Paused?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnStopped()
    {
      Stopped?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPlaying()
    {
      Playing?.Invoke(this, EventArgs.Empty);
    }


    protected virtual void OnTimeChanged(PlayerTimeChangedArgs e)
    {
      TimeChanged?.Invoke(this, e);
    }

    protected virtual void OnBuffering(PlayerBufferingEventArgs e)
    {
      Buffering?.Invoke(this, e);
    }

    #endregion

    public override void Dispose()
    {
      libVLC?.Dispose();
    }

    protected virtual void OnMuted()
    {
      Muted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnUnmuted()
    {
      Unmuted?.Invoke(this, EventArgs.Empty);
    }
  }
}
