using System;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.SoundItems;
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
      for(int i=0;i<1000;i++) Assert.True(await view.TryToRefreshUpdateLyrics());
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
    [Fact]
    public async Task ExplicitRefreshWithoutProviderMetadataPreservesUsableLyrics()
    {
      using var fixture=new SavedSongViewFixture();
      var song=new Song {ItemModel=new SoundItem {Id=4,IsAutomaticLyricsFindEnabled=true}};
      var view=fixture.CreateDirect(song);
      var timeline=LyricsAnimationFixture.Create(100000);
      view.LRCFile=timeline;
      Assert.True(await view.TryToRefreshUpdateLyrics(forceRefresh:true));
      Assert.Same(timeline,view.LRCFile);
    }
    private sealed class ControlledSong : SongInPlayListViewModel
    {
      internal readonly TaskCompletionSource<bool> Release=new TaskCompletionSource<bool>();
      internal int Calls;
      internal LRCFileViewModel Replacement;
      internal ControlledSong(Song model,SavedSongViewFixture fixture):base(fixture.Events,fixture.Albums,fixture.Artists,
        fixture.Downloader,fixture.Cloud,model,fixture.Logger,fixture.Storage,fixture.Windows,fixture.Factory,fixture.Lyrics)
      {
        // These tests only need the presence of metadata; provider I/O is controlled below.
        AlbumViewModel=(AlbumViewModel)FormatterServices.GetUninitializedObject(typeof(AlbumViewModel));
        ArtistViewModel=(ArtistViewModel)FormatterServices.GetUninitializedObject(typeof(ArtistViewModel));
      }
      protected override async Task LoadLRCFromPCloud()
      {
        Calls++;await Release.Task;
        if(Replacement!=null)LRCFile=Replacement;
      }
    }
    [Fact]
    public async Task CachedTextStaysVisibleDuringOptionalSynchronizedLyricsLoading()
    {
      using var fixture=new SavedSongViewFixture();
      var text=new string('x',100000);
      var model=new Song {ItemModel=new SoundItem {Id=5,IsAutomaticLyricsFindEnabled=true},Chartlyrics_Lyric=text};
      var view=new ControlledSong(model,fixture){Replacement=LyricsAnimationFixture.Create(100000)};
      var pending=view.TryToRefreshUpdateLyrics();
      Assert.Equal(1,view.Calls);Assert.False(pending.IsCompleted);
      Assert.Equal(text,view.Lyrics);Assert.Equal(text,model.Chartlyrics_Lyric);
      view.Release.SetResult(true);Assert.True(await pending);
      Assert.Same(view.Replacement,view.LRCFile);
    }
    [Fact]
    public async Task MissingOptionalSynchronizedLyricsKeepTheCachedText()
    {
      using var fixture=new SavedSongViewFixture();
      var text=new string('x',100000);
      var model=new Song {ItemModel=new SoundItem {Id=6,IsAutomaticLyricsFindEnabled=true},Chartlyrics_Lyric=text};
      var view=new ControlledSong(model,fixture);
      var pending=view.TryToRefreshUpdateLyrics();
      Assert.Equal(1,view.Calls);Assert.Equal(text,view.Lyrics);
      view.Release.SetResult(true);Assert.True(await pending);Assert.Equal(text,view.Lyrics);
    }
    [Fact]
    public void ManualRefreshCommandCanReplaceACachedTimeline()=>Sta.Run(()=>
    {
      using var fixture=new SavedSongViewFixture();
      var model=new Song {ItemModel=new SoundItem {Id=7,IsAutomaticLyricsFindEnabled=true}};
      var view=new ControlledSong(model,fixture){Replacement=LyricsAnimationFixture.Create(100000)};
      view.LRCFile=LyricsAnimationFixture.Create(100000);
      view.Refresh.Execute(null);Assert.Equal(1,view.Calls);
      view.Release.SetResult(true);
      FileBrowserFixture.WaitUntil(()=>ReferenceEquals(view.Replacement,view.LRCFile),TimeSpan.FromSeconds(10));
    });
  }
}
