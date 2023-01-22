using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  [Serializable]
  public class Playlist<TPlaylistItem> : DomainEntity, IPlaylist<TPlaylistItem>
  {
    public string Name { get; set; }
    public string CoverPath { get; set; }
    public long? HashCode { get; set; }
    public int? ItemCount { get; set; }
    public int LastItemIndex { get; set; }
    public TimeSpan TotalPlayedTime { get; set; }
    public DateTime LastPlayed { get; set; }
    public List<TPlaylistItem> PlaylistItems { get; set; }
    public int IdActualItem { get; set; }
    public TPlaylistItem ActualItem { get; set; }
    public bool IsUserCreated { get; set; }
    

    public virtual void Update(IPlaylist other)
    {
      Name = other.Name;
      LastItemIndex = other.LastItemIndex;
      CoverPath = other.CoverPath;

      LastPlayed = other.LastPlayed;
      HashCode = other.HashCode;
      ItemCount = other.ItemCount;
      TotalPlayedTime = other.TotalPlayedTime;
      IsUserCreated = other.IsUserCreated;
      Modified = other.Modified;

      if (other is IPlaylist<TPlaylistItem> playlist)
      {
        ActualItem = playlist.ActualItem;
        IdActualItem = playlist.IdActualItem;
      }
    }
  }


  [Serializable]
  public class FilePlaylist<TPlaylistItems> : Playlist<TPlaylistItems>, IFilePlaylist<TPlaylistItems>
  {
    public bool IsReapting { get; set; }
    public bool IsShuffle { get; set; }
    public float LastItemElapsedTime { get; set; }
    public bool WatchFolder { get; set; }


    public override void Update(IPlaylist other)
    {
      base.Update(other);

      if (other is IFilePlaylist filePlaylistOther)
      {
        IsReapting = filePlaylistOther.IsReapting;
        IsShuffle = filePlaylistOther.IsShuffle;
        LastItemElapsedTime = filePlaylistOther.LastItemElapsedTime;
        WatchFolder = filePlaylistOther.WatchFolder;
      }
    }
  }
}