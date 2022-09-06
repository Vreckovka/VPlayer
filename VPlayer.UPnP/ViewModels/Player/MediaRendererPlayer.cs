using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using DLNA;
using NAudio.Wave;
using UPnP.Device;
using VCore.Standard;
using VPlayer.WindowsPlayer.Players;

namespace VPlayer.UPnP.ViewModels.Player
{
  public class UNpNMedia : IMedia
  {
    public event EventHandler<MediaDurationChangedArgs> DurationChanged;
    public long Duration { get; } 

  }

  public class MediaRendererPlayer : ViewModel<MediaRenderer>, IPlayer
  {

    private readonly MediaRenderer model;
    private readonly DLNADevice dLNADevice;
    private StreamingMediaServer streamingMediaServer;
    private SerialDisposable positionDisposable = new SerialDisposable();
    public MediaRendererPlayer(MediaRenderer model) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));

      dLNADevice = new DLNADevice(Model.PresentationURL);
      dLNADevice.ControlURL = "upnp/control/rendertransport1";
      streamingMediaServer = new StreamingMediaServer(GetLocalIPAddress(), 2876);

      if (Application.Current != null)
      {
        Application.Current.MainWindow.Closing += MainWindow_Closing;
      }

      streamingMediaServer.Start();
    }

    public static string GetLocalIPAddress()
    {
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        socket.Connect("8.8.8.8", 65530);
        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

        return endPoint.Address.ToString();
      }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Dispose();
    }

  

    public int Volume { get; set; }
    public long Length { get; }

    #region Position

    private float position;

    public float Position
    {
      get { return position; }
      set
      {
        SetPosition(value, true);
      }
    }

    #endregion


    public bool IsPlaying { get; private set; }

    public bool IsMuted
    {
      get
      {
        return false;
      }
    }

    public event EventHandler EncounteredError;
    public event EventHandler EndReached;
    public event EventHandler Paused;
    public event EventHandler Stopped;
    public event EventHandler Playing;
    public event EventHandler<PlayerBufferingEventArgs> Buffering;
    public event EventHandler<PlayerTimeChangedArgs> TimeChanged;
    public IMedia Media { get; set; }

    #region Initilize

    public Task Initilize()
    {
      return Task.Run(() =>
      {
        model.Init();
      });
    }

    #endregion

    #region Play

    public void Play()
    {
      positionDisposable.Disposable?.Dispose();

      dLNADevice.StopPlay(0);

      var response = dLNADevice.StartPlay(0);
      IsPlaying = true;
      OnPlaying();

      positionDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(500)).Subscribe(x =>
      {
        ObserveTimeChanged();
      });
    }

    #endregion


    public void Pause()
    {
      dLNADevice.Pause(0);
      OnPaused();
      IsPlaying = false;
      OnPaused();
    }

    public void Stop()
    {
      dLNADevice.StopPlay(0);
      positionDisposable.Disposable?.Dispose();
      IsPlaying = false;
      OnStopped();
    }

    public void Reload()
    {
      throw new NotImplementedException();
    }

    public void ToggleMute()
    {
      
    }

    #region SetPosition

    private bool isSeeking = false;
    private object batton = new object();
    private float lastSeek = 0;

    private void SetPosition(float position, bool fromEvent)
    {
      lock (batton)
      {
        this.position = position;

        if (fromEvent)
        {
          if (!isSeeking && Math.Abs(lastSeek - position) > 0.05 && acutalMediaDuration != null)
          {
            isSeeking = true;
            lastSeek = position;
            positionDisposable.Disposable?.Dispose();
            var newPosition = TimeSpan.FromMilliseconds(acutalMediaDuration.Value.TotalMilliseconds * position);
            OnBuffering(new PlayerBufferingEventArgs() { Cache = 0 });

            dLNADevice.Seek(0, newPosition.ToString(@"hh\:mm\:ss"));

            positionDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(500)).Subscribe(x =>
            {
              ObserveTimeChanged();
            });

            isSeeking = false;
          }
        }

        RaisePropertyChanged();
      }
    }

    #endregion

    #region ObserveTimeChanged

    private object endLock = new object();
    private bool wasEndReached = false;
    private TimeSpan? acutalMediaDuration;
    private int lastBufferingValue = 0;
    private IMedia lastPlayedMedia;

    private async void ObserveTimeChanged()
    {
      var positionInfo = await Model.GetPositionInfoAsync();

      if (positionInfo == null)
      {
        return;
      }

      string trackPositionString = positionInfo.GetArgumentValue("RelTime");
      string trackDurationString = positionInfo.GetArgumentValue("TrackDuration");


      if (!string.IsNullOrEmpty(trackPositionString) && IsPlaying)
      {
        if (lastBufferingValue == 0)
        {
          OnBuffering(new PlayerBufferingEventArgs() { Cache = 100 });
        }
       
        var actualPosition = TimeSpan.Parse(trackPositionString);
        acutalMediaDuration = TimeSpan.Parse(trackDurationString);

        var newPosition = (float)((float)actualPosition.TotalMilliseconds * 100 / actualPosition.TotalMilliseconds) / 100;

        SetPosition(newPosition, false);

        OnTimeChanged(new PlayerTimeChangedArgs()
        {
          Time = (long)actualPosition.TotalMilliseconds
        });

        if (!string.IsNullOrEmpty(trackPositionString)
            && trackPositionString == trackDurationString &&
            lastPlayedMedia == Media)
        {
          lock (endLock)
          {
            if (!wasEndReached)
            {
              string status = dLNADevice.GetTransportInfo();
              string currentStatus = status.ChopOffBefore("<CurrentTransportState>").Trim().ChopOffAfter("</CurrentTransportState>");

              if (currentStatus == "STOPPED" && IsPlaying && !wasNewMediaRequest)
              {
                wasEndReached = true;

                OnEndReached();

                positionDisposable.Disposable?.Dispose();
              }
            }
          }
        }

        lastPlayedMedia = Media;
      }
    }

    #endregion

    #region SetNewMedia

    private string lastUri;
    private bool wasNewMediaRequest;
    public Task SetNewMedia(Uri source)
    {
      return Task.Run(async () =>
      {
        try
        {
          wasNewMediaRequest = true;

          OnBuffering(new PlayerBufferingEventArgs() { Cache = 0 });

          positionDisposable.Disposable?.Dispose();

          dLNADevice?.StopPlay(0);
          acutalMediaDuration = null;
          wasEndReached = false;

          SetPosition(0, false);

          OnTimeChanged(new PlayerTimeChangedArgs()
          {
            Time = 0
          });

          if (source != null)
          {
            string path = source.LocalPath;
            string streamUri = streamingMediaServer.Stream;

            if (File.Exists(path))
            {
              await streamingMediaServer.LoadFile(path);
            }
            else
            {
              await streamingMediaServer.PlayStream(source.AbsoluteUri);
              streamUri = source.AbsoluteUri;
            }

            if (lastUri != streamUri)
            {
              lastUri = streamUri;

              var response = dLNADevice?.UploadFileToPlay(streamUri);

              Media = new UNpNMedia();
            }

            if (IsPlaying)
            {
              Play();
            }
          }
        }
        finally
        {
          wasNewMediaRequest = false;
        }
      });
    }

    #endregion

    #region OnTimeChanged

    protected virtual void OnTimeChanged(PlayerTimeChangedArgs e)
    {
      Task.Run(() =>
      {
        TimeChanged?.Invoke(this, e);
      });
    }

    #endregion

    #region OnEndReached

    protected virtual void OnEndReached()
    {
      EndReached?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();
      streamingMediaServer.Running = false;
      positionDisposable?.Dispose();
      dLNADevice?.StopPlay(0);
    }

    #endregion

    protected virtual void OnPlaying()
    {
      Playing?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPaused()
    {
      Paused?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnStopped()
    {
      Stopped?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnBuffering(PlayerBufferingEventArgs e)
    {
      Buffering?.Invoke(this, e);
      lastBufferingValue = (int)e.Cache;
    }
  }
}
