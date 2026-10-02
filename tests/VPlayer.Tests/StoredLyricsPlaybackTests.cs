using System;
using System.Threading.Tasks;
using VCore.WPF.LRC;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.TestSupport;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class StoredLyricsPlaybackTests
  {
    [Fact]
    public async Task AutomaticRefreshKeepsTheLoadedTimelineWhenAlbumMetadataIsMissing()
    {
      using var fixture=new SavedSongViewFixture();
      var song=new Song {ItemModel=new SoundItem {Id=1,IsAutomaticLyricsFindEnabled=true}};
      var view=fixture.CreateDirect(song);
      var timeline=LyricsAnimationFixture.Create(100000);
      view.LRCFile=timeline;
      timeline.SetActualLine(TimeSpan.FromSeconds(90000));
      var active=timeline.ActualLine;
      Assert.True(await view.TryToRefreshUpdateLyrics());
      Assert.Same(timeline,view.LRCFile);
      Assert.Same(active,view.LRCFile.ActualLine);
      Assert.Equal(100000,view.LRCFile.AllLine.Count);
    }
    [Fact]
    public async Task AutomaticRefreshKeepsStoredTextWhenAlbumMetadataIsMissing()
    {
      using var fixture=new SavedSongViewFixture();
      var text=new string('x',100000);
      var song=new Song {ItemModel=new SoundItem {Id=2,IsAutomaticLyricsFindEnabled=true},Chartlyrics_Lyric=text};
      var view=fixture.CreateDirect(song);
      await view.TryToRefreshUpdateLyrics();
      Assert.Equal(text,view.Lyrics);
      Assert.Equal(text,song.Chartlyrics_Lyric);
    }
    [Fact]
    public async Task StoredSynchronizedLyricsLoadBeforeRequiringAlbumOrArtistMetadata()
    {
      using var fixture=new SavedSongViewFixture();
      var song=new Song {ItemModel=new SoundItem {Id=3,IsAutomaticLyricsFindEnabled=true},
        LRCLyrics=((int)LRCProviders.Local)+";\n[00:00.00]Stored first line\n[00:01.00]Stored second line"};
      var view=fixture.CreateDirect(song);
      Assert.True(await view.TryToRefreshUpdateLyrics());
      Assert.NotNull(view.LRCFile);
      Assert.Contains(view.LRCFile.AllLine,x=>x.Text=="Stored first line");
    }
  }
}
