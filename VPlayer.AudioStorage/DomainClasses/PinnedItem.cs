using System;
using System.Collections.Generic;
using System.Text;

namespace VPlayer.AudioStorage.DomainClasses
{
  public enum PinnedType
  {
    SoundPlaylist,
    VideoPlaylist,
    SoundFolder,
    VideoFolder,
    SoundFile,
    VideoFile,
    Artist,
    Album,
    None
  }

  public class PinnedItem : DomainEntity
  {
    public PinnedType PinnedType { get; set; }

    /// <summary>
    /// You can send Json here
    /// </summary>
    public string Description { get; set; }
  }
}
