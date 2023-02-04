using System;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  [Serializable]
  public class TvShowEpisode : DomainEntity, IUpdateable<TvShowEpisode>, IDownloadableEntity, IPlayableModel
  {
    public TvShowEpisode()
    {

    }

    public TvShowEpisode(TvShow tvShow)
    {
      TvShow = tvShow;
    }


    public InfoDownloadStatus InfoDownloadStatus { get; set; }
    public TvShow TvShow { get; set; }
    public TvShowSeason TvShowSeason { get; set; }
    public VideoItem VideoItem { get; set; }
    public int EpisodeNumber { get; set; }

    public string Source
    {
      get { return VideoItem?.Source; }
      set
      {
        if (VideoItem != null)
          VideoItem.Source = value;
      }
    }

    public int Duration
    {
      get
      {
        if (VideoItem != null)
          return VideoItem.Duration;


        return 0;
      }
      set { if (VideoItem != null) VideoItem.Duration = value; }
    }

    public long Length
    {
      get
      {
        if (VideoItem != null)
          return VideoItem.Length;

        return 0;
      }
      set { if (VideoItem != null) VideoItem.Length = Length; }
    }

    public string Name
    {
      get { return VideoItem?.Name; }
      set { if (VideoItem != null) VideoItem.Name = Name; }
    }

    public bool IsFavorite
    {
      get
      {
         if (VideoItem != null)
          return VideoItem.IsFavorite;

         return false;
      }
      set { if (VideoItem != null) VideoItem.IsFavorite = IsFavorite; }
    }


    public void Update(TvShowEpisode other)
    {
      if (other.VideoItem != null)
        VideoItem?.Update(other.VideoItem);

      EpisodeNumber = other.EpisodeNumber;
    }


  }
}