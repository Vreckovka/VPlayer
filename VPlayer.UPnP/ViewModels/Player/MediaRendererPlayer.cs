using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DLNA;
using UPnP.Device;
using VCore.Standard;
using VPlayer.WindowsPlayer.Players;

namespace VPlayer.UPnP.ViewModels.Player
{
  public class MediaRendererPlayer : ViewModel<MediaRenderer>, IPlayer
  {
    private readonly MediaRenderer model;
    private readonly DLNADevice dLNADevice;

    public MediaRendererPlayer(MediaRenderer model) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));

      dLNADevice = new DLNADevice(Model.PresentationURL);
      dLNADevice.ControlURL = "upnp/control/rendertransport1";
    }

    public int Volume { get; set; }
    public long Length { get; }
    public float Position { get; set; }
    public bool IsPlaying { get; }

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

    public void Play()
    {
      var response = dLNADevice.StartPlay(0);
    }

    public void Pause()
    {
      throw new NotImplementedException();
    }

    public void Stop()
    {
      throw new NotImplementedException();
    }

    public void Reload()
    {
      throw new NotImplementedException();
    }

    #region SetNewMedia

    public void SetNewMedia(Uri source)
    {
      var response = dLNADevice.UploadFileToPlay(source.AbsoluteUri);
    }

    #endregion
  }
}
