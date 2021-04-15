using System;
using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Playlist<TPlaylistItems> : DomainEntity, IPlaylist<TPlaylistItems>
  {
    public string Name { get; set; }
    public long? HashCode { get; set; }
    public int? ItemCount { get; set; }
    public int LastItemIndex { get; set; }
    public TimeSpan TotalPlayedTime { get; set; }
    public DateTime LastPlayed { get; set; }
    public List<TPlaylistItems> PlaylistItems { get; set; }
    public bool IsUserCreated { get; set; }

    public void Update(IPlaylist other)
    {
      Name = other.Name;
      LastItemIndex = other.LastItemIndex;
      LastPlayed = other.LastPlayed;
      HashCode = other.HashCode;
      ItemCount = other.ItemCount;
      TotalPlayedTime = other.TotalPlayedTime;
      IsUserCreated = other.IsUserCreated;
    }
  }

  public class FilePlaylist<TPlaylistItems> : Playlist<TPlaylistItems>, IFilePlaylist<TPlaylistItems>
  {
    public bool IsReapting { get; set; }
    public bool IsShuffle { get; set; }
    public float LastItemElapsedTime { get; set; }


    public void Update(IFilePlaylist other)
    {
      base.Update(other);

      IsReapting = other.IsReapting;
      IsShuffle = other.IsShuffle;
      LastItemElapsedTime = other.LastItemElapsedTime;
    }
  }
}