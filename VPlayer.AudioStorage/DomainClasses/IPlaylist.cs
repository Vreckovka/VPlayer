using System;
using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IPlaylist<TModel> : IPlaylist
  {
    List<TModel> PlaylistItems { get; set; }
  }

  public interface IPlaylist : IUpdateable<IPlaylist>, INamedEntity, ITrackable
  {
    long? HashCode { get; set; }
    bool IsUserCreated { get; set; }
    int? ItemCount { get; set; }
    DateTime LastPlayed { get; set; }
    bool IsReapting { get; set; }
    bool IsShuffle { get; set; }
    float LastItemElapsedTime { get; set; }
    int LastItemIndex { get; set; }
    TimeSpan TotalPlayedTime { get; set; }
  }
}