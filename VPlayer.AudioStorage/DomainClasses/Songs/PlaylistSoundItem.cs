﻿using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistSoundItem : DomainEntity, IItemInPlaylist<SoundItem>
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public SoundItem ReferencedItem { get; set; }
  }
}