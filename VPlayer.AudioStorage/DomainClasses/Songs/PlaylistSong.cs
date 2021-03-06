﻿using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistSong : DomainEntity, IItemInPlaylist<Song>
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public Song ReferencedItem { get; set; }
  }
}