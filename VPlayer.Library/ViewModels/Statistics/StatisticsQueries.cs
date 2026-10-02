using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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
      var soundQuery=storage.GetTempRepository<SoundItem>().Where(x=>!x.IsPrivate);
      var videoQuery=storage.GetTempRepository<VideoItem>().Where(x=>!x.IsPrivate);
      var episodeQuery=storage.GetTempRepository<TvShowEpisode>().Where(x=>!x.IsPrivate);
      var sounds=new Ranking(soundQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}));
      var videos=new Ranking(videoQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}));
      var episodes=new Ranking(episodeQuery.Select(x=>new Score {Id=x.Id,Time=x.TimePlayed}));
      var soundIds=sounds.Ids;
      var videoIds=videos.Ids;
      var episodeIds=episodes.Ids;
      var soundModels=soundQuery.Where(x=>soundIds.Contains(x.Id)).Include(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      var videoModels=videoQuery.Where(x=>videoIds.Contains(x.Id)).Include(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      var episodeModels=episodeQuery.Where(x=>episodeIds.Contains(x.Id)).Include(x=>x.VideoItem)
        .ThenInclude(x=>x.FileInfoEntity).OrderBy(x=>x.Id).ToArray();
      var items=soundModels.Cast<DomainEntity>().Concat(videoModels).Concat(episodeModels)
        .OrderByDescending(x=>((IPlayableModel)x).TimePlayed).Take(AllSize).ToArray();
      var soundItems=soundModels.OrderByDescending(x=>x.TimePlayed).Take(CategorySize).Cast<DomainEntity>().ToArray();
      var videoItems=videoModels.OrderByDescending(x=>x.TimePlayed).Take(CategorySize).Cast<DomainEntity>().ToArray();
      var selectedSounds=items.Concat(soundItems).OfType<SoundItem>().Select(x=>x.Id).Distinct().ToArray();
      var songs=storage.GetTempRepository<Song>().Where(x=>selectedSounds.Contains(x.ItemModelId))
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
