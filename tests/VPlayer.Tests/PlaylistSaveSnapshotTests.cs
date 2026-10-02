using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistSaveSnapshotTests
  {
    [Fact]
    public void HugeDuplicateQueuePreservesOccurrencesMetadataAndDetachedState()
    {
      var file=new FileInfoEntity {Id=72,Name="Original",Title="Stored title",Album="Stored album",Artist="Stored artist",
        Extension=".flac",Source="D:/music/song.flac",FullName="D:/music/song.flac",Length=123456,Indentificator="stable file",
        Created=new DateTime(2020,2,3),Modified=new DateTime(2021,4,5)};
      var shared=new SoundItem {Id=19,Duration=600,FileInfoEntity=file,VideoPath="D:/clip.mp4",IsFavorite=true,
        IsPrivate=true,TimePlayed=TimeSpan.FromDays(20),IsAutomaticLyricsFindEnabled=false,NormalizedName="stored title",
        Created=new DateTime(2020,2,3),Modified=new DateTime(2021,4,5)};
      var sameIdDifferentObject=new SoundItem {Id=19,Duration=900,FileInfoEntity=file};
      var playlist=new SoundItemFilePlaylist {Id=23,Name="Huge private cloud queue",PlaylistType=PlaylistType.Cloud,
        IsPrivate=true,IsUserCreated=true,IsReapting=true,IsShuffle=true,WatchedFolder="D:/music",CoverPath="D:/cover.png",
        LastItemIndex=99999,LastItemElapsedTime=123.5f,TotalPlayedTime=TimeSpan.FromDays(123),LastPlayed=new DateTime(2026,1,2),
        HashCode=long.MaxValue,ItemCount=100000,Created=new DateTime(2020,2,3),Modified=new DateTime(2021,4,5),
        PlaylistItems=Enumerable.Range(0,100000).Select(i=>new PlaylistSoundItem {Id=i+1,IdReferencedItem=19,
          OrderInPlaylist=i,ReferencedItem=i==50000?sameIdDifferentObject:shared,Created=new DateTime(2020,2,3)}).ToList()};
      playlist.ActualItem=playlist.PlaylistItems[99999];
      playlist.ActualItemId=playlist.ActualItem.Id;
      var copy=PlaylistSaveSnapshot.Create(playlist);
      EqualScalars(playlist,copy);
      Assert.NotSame(playlist.PlaylistItems,copy.PlaylistItems);
      Assert.Same(copy.PlaylistItems[99999],copy.ActualItem);
      Assert.Same(copy.PlaylistItems[0].ReferencedItem,copy.PlaylistItems[99999].ReferencedItem);
      Assert.NotSame(copy.PlaylistItems[0].ReferencedItem,copy.PlaylistItems[50000].ReferencedItem);
      Assert.Same(copy.PlaylistItems[0].ReferencedItem.FileInfoEntity,copy.PlaylistItems[50000].ReferencedItem.FileInfoEntity);
      for(int i=0;i<copy.PlaylistItems.Count;i++)
      {
        Assert.NotSame(playlist.PlaylistItems[i],copy.PlaylistItems[i]);
        EqualScalars(playlist.PlaylistItems[i],copy.PlaylistItems[i]);
        EqualScalars(playlist.PlaylistItems[i].ReferencedItem,copy.PlaylistItems[i].ReferencedItem);
      }
      Assert.NotSame(shared,copy.ActualItem.ReferencedItem);
      Assert.NotSame(file,copy.ActualItem.ReferencedItem.FileInfoEntity);
      EqualScalars(file,copy.ActualItem.ReferencedItem.FileInfoEntity);
      file.Title="changed after request";
      shared.TimePlayed=TimeSpan.Zero;
      playlist.PlaylistItems[99999].OrderInPlaylist=-1;
      playlist.PlaylistItems.Clear();
      Assert.Equal(100000,copy.PlaylistItems.Count);
      Assert.Equal(99999,copy.ActualItem.OrderInPlaylist);
      Assert.Equal("Stored title",copy.ActualItem.ReferencedItem.Name);
      Assert.Equal(TimeSpan.FromDays(20),copy.ActualItem.ReferencedItem.TimePlayed);
    }

    [Fact]
    public void NewCurrentOccurrenceAndRepeatedRowsKeepReferenceIdentity()
    {
      var row=new PlaylistSoundItem {IdReferencedItem=17,ReferencedItem=new SoundItem {Id=17}};
      var playlist=new SoundItemFilePlaylist {PlaylistItems=new List<PlaylistSoundItem> {row,row},ActualItem=row};
      var copy=PlaylistSaveSnapshot.Create(playlist);
      Assert.Same(copy.ActualItem,copy.PlaylistItems[0]);
      Assert.Same(copy.PlaylistItems[0],copy.PlaylistItems[1]);
      Assert.Null(copy.ActualItemId);
      Assert.Null(copy.ActualItem.ReferencedItem.FileInfoEntity);
    }

    [Fact]
    public void VideoMetadataAndDetachedCurrentRowArePreserved()
    {
      var item=new VideoItem {Id=31,AudioTrack=4,SubtitleTrack=7,AspectRatio="16:9",CropRatio="4:3",
        FileInfoEntity=new FileInfoEntity {Title="Video",Source="D:/video.mkv"}};
      var playlist=new VideoFilePlaylist {Id=2,PlaylistItems=new List<PlaylistVideoItem>(),
        ActualItem=new PlaylistVideoItem {Id=8,IdReferencedItem=31,ReferencedItem=item},ActualItemId=8};
      var copy=PlaylistSaveSnapshot.Create(playlist);
      Assert.Empty(copy.PlaylistItems);
      Assert.NotSame(playlist.ActualItem,copy.ActualItem);
      Assert.NotSame(item,copy.ActualItem.ReferencedItem);
      EqualScalars(item,copy.ActualItem.ReferencedItem);
      copy.ActualItem.ReferencedItem.FileInfoEntity.Title="changed snapshot";
      Assert.Equal("Video",item.FileInfoEntity.Title);
    }

    [Fact]
    public void NullAndUnloadedRowsRemainDistinctFromAnEmptyQueue()
    {
      Assert.Null(PlaylistSaveSnapshot.Create<SoundItemFilePlaylist>(null));
      Assert.Null(PlaylistSaveSnapshot.Create(new SoundItemFilePlaylist()).PlaylistItems);
      Assert.Empty(PlaylistSaveSnapshot.Create(new SoundItemFilePlaylist {PlaylistItems=new List<PlaylistSoundItem>()}).PlaylistItems);
    }

    private static void EqualScalars(object source,object copy)
    {
      foreach(var property in source.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public)
        .Where(x=>x.GetIndexParameters().Length==0 && (x.PropertyType.IsValueType || x.PropertyType==typeof(string))))
        Assert.Equal(property.GetValue(source),property.GetValue(copy));
    }
  }
}
