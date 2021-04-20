using System;
using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IPlaylist : INamedEntity, ITrackable, IUpdateable<IPlaylist>
  {
    int Id { get; set; }
    long? HashCode { get; set; }
    int? ItemCount { get; set; }
    TimeSpan TotalPlayedTime { get; set; }
    int LastItemIndex { get; set; }
    bool IsUserCreated { get; set; }
    DateTime LastPlayed { get; set; }
  }

  public interface IPlaylist<TModel> : IPlaylist
  {
    List<TModel> PlaylistItems { get; set; }
  }

  public interface IFilePlaylist<TModel> : IPlaylist<TModel>, IFilePlaylist
  {
  }

  public interface IFilePlaylist : IPlaylist
  {
    bool IsReapting { get; set; }
    bool IsShuffle { get; set; }
    float LastItemElapsedTime { get; set; }

  }
}