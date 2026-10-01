using System;
using System.Collections.Generic;
using System.Linq;
using VCore.WPF.LRC.Domain;
using VPlayer.Core.ViewModels.SoundItems;
using Xunit;

namespace VPlayer.Tests
{
  public class LyricsTimelineTests
  {
    [Fact]
    public void DuplicateTimestampsUseFirstDisplayOccurrence()
    {
      var lyrics = LyricsTests.Create(0,10,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(10));
      Assert.Same(lyrics.AllLine[1],lyrics.ActualLine);
    }
    [Fact]
    public void UntimedLinesDoNotInterruptSelection()
    {
      var lyrics = LyricsTests.Create(0,10,20);
      lyrics.AllLine.Insert(2,new LRCLyricLineViewModel(new LRCLyricLine {Text="Untimed"}));
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      Assert.Same(lyrics.AllLine[1],lyrics.ActualLine);
    }
    [Fact]
    public void ReplacingLyricsRebuildsTimeline()
    {
      var lyrics = LyricsTests.Create(0,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      var old = lyrics.ActualLine;
      lyrics.AllLine = new List<LRCLyricLineViewModel> {new LRCLyricLineViewModel(new LRCLyricLine {Timestamp=TimeSpan.Zero,Text="New"})};
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      Assert.Equal("New",lyrics.ActualLine.Text);
      Assert.False(old.IsActual);
    }
    [Fact]
    public void EditedTimestampCanInvalidateTimeline()
    {
      var lyrics = LyricsTests.Create(0,10,20);
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      lyrics.AllLine[1].Model.Timestamp = TimeSpan.FromSeconds(30);
      lyrics.InvalidateTimeline();
      lyrics.SetActualLine(TimeSpan.FromSeconds(15));
      Assert.Equal(TimeSpan.Zero,lyrics.ActualLine.Model.Timestamp);
    }
    [Fact]
    public void SeeksAndOffsetsMatchReferenceSelection()
    {
      var lyrics = LyricsTests.Create(0,50,10,30,10,20);
      foreach(var offset in new[] {-5000.0,0,7000})
      {
        lyrics.TimeAdjustment = offset;
        foreach(var seconds in new[] {60.0,11,0,-10,25,15,50,10})
        {
          var time = TimeSpan.FromSeconds(seconds);
          var expected = lyrics.AllLine.Where(x=>x.Model.Timestamp.HasValue && x.Model.Timestamp.Value.TotalMilliseconds + offset <= time.TotalMilliseconds)
            .OrderByDescending(x=>x.Model.Timestamp).FirstOrDefault();
          lyrics.SetActualLine(time);
          Assert.Same(expected,lyrics.ActualLine);
          Assert.Equal(expected==null ? 0 : 1,lyrics.AllLine.Count(x=>x.IsActual));
        }
      }
    }
    [Fact]
    public void WarmPlaybackTicksWithinLineAllocateNoMemory()
    {
      var lyrics = LyricsTests.Create(0,10,20);
      var time = TimeSpan.FromSeconds(15);
      lyrics.SetActualLine(time);
      for(var i=0;i<100;i++) lyrics.SetActualLine(time);
      var before = GC.GetAllocatedBytesForCurrentThread();
      for(var i=0;i<10000;i++) lyrics.SetActualLine(time);
      Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
  }
}
