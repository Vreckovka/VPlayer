using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Logger;
using VCore.Standard;
using VPlayer.Core.Providers;

namespace VPlayer.WindowsPlayer.Players
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
      get { return MediaPlayer.Volume; }
      set
      {
        if (value != MediaPlayer.Volume)
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

    public async Task Initilize()
    {
      var result = await vlcProvider.InitlizeVlc();

      MediaPlayer = result.Key;

      libVLC = result.Value;

      HookToVlcEvents();
    }

    #endregion

    #region Reload

    public void Reload()
    {
      libVLC?.Dispose();
      libVLC = new LibVLC();
    }

    #endregion

    #region HookToVlcEvents

    protected void HookToVlcEvents()
    {
      Volume = MediaPlayer.Volume;

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        OnEncounteredError();
      };


      MediaPlayer.EndReached += (sender, e) => { OnEndReached(); };

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

    }


    #endregion

    public void Play()
    {
      MediaPlayer?.Play();
    }

    public void Pause()
    {
      MediaPlayer?.Pause();
    }

    public void Stop()
    {
      MediaPlayer?.Stop();
    }

    #region SetNewMedia

    public Task SetNewMedia(Uri source)
    {
      return Task.Run(() =>
      {
        if(source != null)
        {
          var media = new Media(libVLC, source);

          Media = new VLCMedia(media);

          MediaPlayer.Media = media;
        }
        else
        {
          MediaPlayer.Media = null;
        }
      
      });
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
  }
}
