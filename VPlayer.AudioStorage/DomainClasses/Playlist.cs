using System;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Playlist : DomainEntity, INamedEntity, IPlaylist
  {
    
    public string Name { get; set; }
    public long? HashCode { get; set; }
    public int? ItemCount { get; set; }
    public bool IsReapting { get; set; }
    public bool IsShuffle { get; set; }
    public float LastItemElapsedTime { get; set; }
    public int LastItemIndex { get; set; }
    public bool IsUserCreated { get; set; }
    public DateTime LastPlayed { get; set; }

    public void Update(Playlist other)
    {
      Id = other.Id;
      Name = other.Name;
      IsReapting = other.IsReapting;
      IsShuffle = other.IsShuffle;
      LastItemElapsedTime = other.LastItemElapsedTime;
      LastItemIndex = other.LastItemIndex;
      IsUserCreated = other.IsUserCreated;
      LastPlayed = other.LastPlayed;
      HashCode = other.HashCode;
      ItemCount = other.ItemCount;
    }
  }
}