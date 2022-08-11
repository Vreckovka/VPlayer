using System;
using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public enum PlaylistType
  {
    Local,
    Cloud
  }

  [Serializable]
  public class SoundItemFilePlaylist : FilePlaylist<PlaylistSoundItem>
  {
    public PlaylistType PlaylistType { get; set; }
  }
}

