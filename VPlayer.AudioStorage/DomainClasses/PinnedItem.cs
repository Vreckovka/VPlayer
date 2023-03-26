using System;
using System.Collections.Generic;
using System.Text;
using VCore.Standard.Modularity.Interfaces;

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

  public class PinnedItem : DomainEntity, IUpdateable<PinnedItem>
  {
    public PinnedType PinnedType { get; set; }

    /// <summary>
    /// You can send Json here
    /// </summary>
    public string Description { get; set; }

    public int OrderNumber { get; set; }
    public void Update(PinnedItem other)
    {
      if(other != null)
      {
        PinnedType = other.PinnedType;
        Description = other.Description;
        OrderNumber = other.OrderNumber;
      }
     
    }
  }
}
