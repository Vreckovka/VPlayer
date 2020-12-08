using System;
using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Playlist : DomainEntity, INamedEntity
  {
    public virtual List<PlaylistSong> PlaylistSongs { get; set; }
    public string Name { get; set; }
    public long? SongsInPlaylitsHashCode { get; set; }
    public int? SongCount { get; set; }
    public bool IsReapting { get; set; }
    public bool IsShuffle { get; set; }
    public float LastSongElapsedTime { get; set; }
    public int LastSongIndex { get; set; }
    public bool IsUserCreated { get; set; }
    public DateTime LastPlayed { get; set; }

    public void Update(Playlist other)
    {
      Id = other.Id;
      Name = other.Name;
      IsReapting = other.IsReapting;
      IsShuffle = other.IsShuffle;
      LastSongElapsedTime = other.LastSongElapsedTime;
      LastSongIndex = other.LastSongIndex;
      IsUserCreated = other.IsUserCreated;
      LastPlayed = other.LastPlayed;
      SongsInPlaylitsHashCode = other.SongsInPlaylitsHashCode;
      SongCount = other.SongCount;

    
    }
  }
}
