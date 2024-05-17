using System;
using System.Threading;
using System.Threading.Tasks;

namespace VPlayer.WindowsPlayer.Players
{
  public interface IPlayer : IDisposable
  {
    public int Volume { get; set; }
    public long Length { get; }
    public float Position { get; set; }
    public bool IsPlaying { get; }
    public bool IsMuted { get; }

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

    void Initilize();
    void Play();
    void Pause();
    void Stop();
    void ToggleMute();
    public void SetNewMedia(Uri source, CancellationToken cancelationToken);

   
  }
}