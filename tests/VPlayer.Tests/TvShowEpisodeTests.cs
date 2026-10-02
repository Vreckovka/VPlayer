using System;
using VPlayer.AudioStorage.DomainClasses.Video;
using Xunit;

namespace VPlayer.Tests
{
  public class TvShowEpisodeTests
  {
    [Fact]
    public void RenamingEpisodesUpdatesTheirReferencedVideos()
    {
      var longName=new string('W',10000)+" – Episode";
      for(int i=0;i<10000;i++)
      {
        var video=new VideoItem {Name="Before"};
        var episode=new TvShowEpisode {VideoItem=video};
        episode.Name=longName;
        Assert.Equal(longName,video.Name);
        Assert.Equal(longName,episode.Name);
        episode.Name=null;
        Assert.Null(video.Name);
      }
    }

    [Fact]
    public void EpisodeLengthsAcceptLargeAndZeroValues()
    {
      for(int i=0;i<10000;i++)
      {
        var video=new VideoItem {Length=123};
        var episode=new TvShowEpisode {VideoItem=video};
        episode.Length=long.MaxValue;
        Assert.Equal(long.MaxValue,video.Length);
        Assert.Equal(long.MaxValue,episode.Length);
        episode.Length=0;
        Assert.Equal(0,video.Length);
      }
    }

    [Fact]
    public void FavoritingAndUnfavoritingEpisodesUpdatesTheirReferencedVideos()
    {
      for(int i=0;i<10000;i++)
      {
        var video=new VideoItem();
        var episode=new TvShowEpisode {VideoItem=video};
        episode.IsFavorite=true;
        Assert.True(video.IsFavorite);
        Assert.True(episode.IsFavorite);
        episode.IsFavorite=false;
        Assert.False(video.IsFavorite);
      }
    }

    [Fact]
    public void EpisodeWithoutVideoKeepsOptionalPropertiesSafe()
    {
      var episode=new TvShowEpisode();
      episode.Name=new string('W',10000);
      episode.Length=long.MaxValue;
      episode.IsFavorite=true;
      episode.Source="file.mp4";
      episode.Duration=int.MaxValue;
      Assert.Null(episode.Name);
      Assert.Null(episode.Source);
      Assert.Equal(0,episode.Length);
      Assert.Equal(0,episode.Duration);
      Assert.False(episode.IsFavorite);
    }
  }
}
