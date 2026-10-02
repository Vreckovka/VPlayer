using System;
using System.Windows;
using VCore.WPF.Behaviors;
using VPlayer.TestSupport;
using Xunit;

namespace VPlayer.Tests
{
  [CollectionDefinition("Lyrics animation",DisableParallelization=true)]
  public class LyricsAnimationCollection {}
  [Collection("Lyrics animation")]
  public class LyricsAnimationTests
  {
    [Fact]
    public void RepeatedTrackChangesReleaseEveryOldLyricsListener()=>Sta.Run(()=>
    {
      var first=LyricsAnimationFixture.Create(100000);var second=LyricsAnimationFixture.Create(100000);
      using var fixture=new LyricsAnimationFixture(first);fixture.Load();
      for(int i=0;i<1000;i++)fixture.View.DataContext=i%2==0?second:first;
      Assert.True(LyricsAnimationFixture.Observed(first));
      Assert.False(LyricsAnimationFixture.Observed(second));
      fixture.Behavior.Detach();
      Assert.False(LyricsAnimationFixture.Observed(first));
    },30);
    [Fact]
    public void ClearingTheTrackReleasesItsLyricsListener()=>Sta.Run(()=>
    {
      var lyrics=LyricsAnimationFixture.Create(100000);
      using var fixture=new LyricsAnimationFixture(lyrics);fixture.Load();
      fixture.View.DataContext=null;
      Assert.False(LyricsAnimationFixture.Observed(lyrics));
    });
    [Fact]
    public void ReloadStopsAndRestoresExactlyTheCurrentTrackListener()=>Sta.Run(()=>
    {
      var lyrics=LyricsAnimationFixture.Create(100000);
      using var fixture=new LyricsAnimationFixture(lyrics);fixture.Load();
      for(int i=0;i<100;i++){fixture.Unload();Assert.False(LyricsAnimationFixture.Observed(lyrics));fixture.Load();Assert.True(LyricsAnimationFixture.Observed(lyrics));}
      fixture.Behavior.Detach();Assert.False(LyricsAnimationFixture.Observed(lyrics));
    },30);
    [Fact]
    public void DataContextChangeFollowedByLoadDoesNotLeaveAListenerAfterDetach()=>Sta.Run(()=>
    {
      var old=LyricsAnimationFixture.Create(100000);var current=LyricsAnimationFixture.Create(100000);
      using var fixture=new LyricsAnimationFixture(old);fixture.Load();
      fixture.View.DataContext=current;fixture.Load();fixture.Behavior.Detach();
      Assert.False(LyricsAnimationFixture.Observed(old));Assert.False(LyricsAnimationFixture.Observed(current));
    });
    [Fact]
    public void SeekJumpStopsTheEarlierScrollAnimation()=>Sta.Run(()=>
    {
      var lyrics=LyricsAnimationFixture.Create(100000);
      using var fixture=new LyricsAnimationFixture(lyrics);fixture.Load();
      lyrics.SetActualLine(TimeSpan.FromSeconds(2));LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(30));
      Assert.True(DependencyPropertyHelper.GetValueSource(fixture.Scroller,ScrollAnimationBehavior.VerticalOffsetProperty).IsAnimated);
      lyrics.SetActualLine(TimeSpan.FromSeconds(90000));LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(30));
      Assert.False(DependencyPropertyHelper.GetValueSource(fixture.Scroller,ScrollAnimationBehavior.VerticalOffsetProperty).IsAnimated);
      Assert.True(fixture.Scroller.VerticalOffset>1000000);
    });
  }
}