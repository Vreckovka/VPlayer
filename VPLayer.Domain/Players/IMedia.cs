using System;

namespace VPlayer.WindowsPlayer.Players
{
  public interface IMedia
  {
    public event EventHandler<MediaDurationChangedArgs> DurationChanged;
  }
}