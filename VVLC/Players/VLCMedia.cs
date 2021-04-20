using System;
using LibVLCSharp.Shared;

namespace VPlayer.WindowsPlayer.Players
{
  public class VLCMedia : IMedia
  {
    private readonly Media media;
    public event EventHandler<MediaDurationChangedArgs> DurationChanged;

    public VLCMedia(Media media)
    {
      this.media = media ?? throw new ArgumentNullException(nameof(media));

      media.DurationChanged += Media_DurationChanged;
    }

    private void Media_DurationChanged(object sender, MediaDurationChangedEventArgs e)
    {
      OnDurationChanged(new MediaDurationChangedArgs()
      {
        Duration = e.Duration
      });
    }

    protected virtual void OnDurationChanged(MediaDurationChangedArgs e)
    {
      DurationChanged?.Invoke(this, e);
    }
  }
}