using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VPLayer.Domain.Diagnostics;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;

namespace VPlayer.Home.ViewModels.Statistics
{
  internal static class StatisticsQueries
  {
    private const int AllSize=30;
    private const int CategorySize=15;

    private sealed class Score
    {
      public int Id {get;set;}
      public TimeSpan Time {get;set;}
    }

    // SQLite cannot order TimeSpan values chronologically. Stream scalar scores,
    // preserving exact ticks for totals and retaining only the highest 30 per type.
    private sealed class Ranking
    {
      private readonly List<Score> top=new List<Score>(AllSize);
      public long Ticks {get;}
      public int[] Ids => top.Select(x=>x.Id).ToArray();
      public Ranking(IEnumerable<Score> scores)
      {
        long ticks=0;
        foreach(var score in scores)
        {
          ticks=checked(ticks+score.Time.Ticks);
          int low=0,high=top.Count;
          while(low<high)
          {
            int middle=(low+high)/2;
            var other=top[middle];
            bool before=score.Time>other.Time || (score.Time==other.Time && score.Id<other.Id);
            if(before) high=middle; else low=middle+1;
          }
          if(low<AllSize)
          {
            top.Insert(low,score);
            if(top.Count>AllSize) top.RemoveAt(AllSize);
          }
        }
        Ticks=ticks;
      }
    }

    public static (DomainEntity[] Items,DomainEntity[] Sounds,DomainEntity[] Videos,TimeSpan Total) LoadItems(IStorageManager storage)
    {
      IQueryable<SoundItem> soundQuery;
      IQueryable<VideoItem> videoQuery;
      IQueryable<TvShowEpisode> episodeQuery;
      using(StartupMeasurements.Measure("Statistics / item repository setup"))
      {
        soundQuery=storage.GetTempRepository<SoundItem>().Where(x=>!x.IsPrivate);
        videoQuery=storage.GetTempRepository<VideoItem>().Where(x=>!x.IsPrivate);
        episodeQuery=storage.GetTempRepository<TvShowEpisode>().Where(x=>!x.IsPrivate);
      }
      Ranking sounds,videos,episodes;
      using(StartupMeasurements.Measure("Statistics / sound scores"))
      {
        var leading=soundQuery.OrderBy(x=>x.Id).Take(AllSize)
          .Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}).ToArray();
        IEnumerable<Score> scores=soundQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed});
        if(leading.Length==AllSize && leading.All(x=>x.Time==TimeSpan.Zero))
        {
          // These are the 30 smallest public IDs, so they cover every zero-time
          // leaderboard slot. Avoid a second full scan when all tracks are played.
          scores=soundQuery.Where(x=>x.TimePlayed!=TimeSpan.Zero)
            .Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}).AsEnumerable()
            // Legacy zero strings can parse as zero without matching SQLite's
            // canonical value. Do not duplicate those IDs from the prefix.
            .Where(x=>x.Time!=TimeSpan.Zero).Concat(leading);
        }
        sounds=new Ranking(scores);
      }
      using(StartupMeasurements.Measure("Statistics / video scores"))
        videos=new Ranking(videoQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}));
      using(StartupMeasurements.Measure("Statistics / episode scores"))
        episodes=new Ranking(episodeQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}));
      var soundIds=sounds.Ids;
      var videoIds=videos.Ids;
      var episodeIds=episodes.Ids;
      SoundItem[] soundModels;
      VideoItem[] videoModels;
      TvShowEpisode[] episodeModels;
      using(StartupMeasurements.Measure("Statistics / sound metadata"))
        soundModels=soundQuery.Where(x=>soundIds.Contains(x.Id)).Include(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      using(StartupMeasurements.Measure("Statistics / video metadata"))
        videoModels=videoQuery.Where(x=>videoIds.Contains(x.Id)).Include(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      using(StartupMeasurements.Measure("Statistics / episode metadata"))
        episodeModels=episodeQuery.Where(x=>episodeIds.Contains(x.Id)).Include(x=>x.VideoItem)
          .ThenInclude(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      var items=soundModels.Cast<DomainEntity>().Concat(videoModels).Concat(episodeModels)
        .OrderByDescending(x=>((IPlayableModel)x).TimePlayed).Take(AllSize).ToArray();
      var soundItems=soundModels.OrderByDescending(x=>x.TimePlayed).Take(CategorySize).Cast<DomainEntity>().ToArray();
      var videoItems=videoModels.OrderByDescending(x=>x.TimePlayed).Take(CategorySize).Cast<DomainEntity>().ToArray();
      var selectedSounds=items.Concat(soundItems).OfType<SoundItem>().Select(x=>x.Id).Distinct().ToArray();
      Dictionary<int,Song> songs;
      using(StartupMeasurements.Measure("Statistics / song metadata"))
        songs=storage.GetTempRepository<Song>().Where(x=>selectedSounds.Contains(x.ItemModelId))
        .Include(x=>x.Album).ThenInclude(x=>x.Artist).Include(x=>x.ItemModel).ThenInclude(x=>x.FileInfoEntity)
        .OrderBy(x=>x.Id).ToArray().GroupBy(x=>x.ItemModelId).ToDictionary(x=>x.Key,x=>x.Last());
      void Enrich(DomainEntity[] rows)
      {
        for(int i=0;i<rows.Length;i++)
          if(rows[i] is SoundItem sound && songs.TryGetValue(sound.Id,out var song)) rows[i]=song;
      }
      Enrich(items);
      Enrich(soundItems);
      return (items,soundItems,videoItems,new TimeSpan(checked(checked(sounds.Ticks+videos.Ticks)+episodes.Ticks)));
    }

    public static (IPlaylist[] Items,TimeSpan Total) LoadPlaylists(IStorageManager storage)
    {
      using var measurement=StartupMeasurements.Measure("Statistics / playlists");
      var soundQuery=storage.GetTempRepository<SoundItemFilePlaylist>().Where(x=>!x.IsPrivate);
      var videoQuery=storage.GetTempRepository<VideoFilePlaylist>().Where(x=>!x.IsPrivate);
      var tvQuery=storage.GetTempRepository<TvPlaylist>().Where(x=>!x.IsPrivate);
      var sounds=new Ranking(soundQuery.Select(x=>new Score {Id=x.Id,Time=x.TotalPlayedTime}));
      var videos=new Ranking(videoQuery.Select(x=>new Score {Id=x.Id,Time=x.TotalPlayedTime}));
      var tv=new Ranking(tvQuery.Select(x=>new Score {Id=x.Id,Time=x.TotalPlayedTime}));
      var soundIds=sounds.Ids;
      var videoIds=videos.Ids;
      var tvIds=tv.Ids;
      var rows=soundQuery.Where(x=>soundIds.Contains(x.Id)).OrderBy(x=>x.Id).ToArray().Cast<IPlaylist>()
        .Concat(videoQuery.Where(x=>videoIds.Contains(x.Id)).OrderBy(x=>x.Id).ToArray())
        .Concat(tvQuery.Where(x=>tvIds.Contains(x.Id)).OrderBy(x=>x.Id).ToArray())
        .OrderByDescending(x=>x.TotalPlayedTime).Take(AllSize).ToArray();
      return (rows,new TimeSpan(checked(checked(sounds.Ticks+videos.Ticks)+tv.Ticks)));
    }
  }
}
