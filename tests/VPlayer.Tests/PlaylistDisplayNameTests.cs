using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Prism.Events;
using VCore.WPF.Interfaces.Managers;
using VPlayer.Core.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Library.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaylistDisplayNameTests
  {
    private sealed class Playlist : PlaylistViewModel<object,SoundItemFilePlaylist,PlaylistSoundItem,SoundItem>
    {
      public Playlist(SoundItemFilePlaylist model) : base(model,new EventAggregator(),
        new Mock<IStorageManager>().Object,new Mock<IWindowManager>().Object) {}
      protected override void OnDetail() {}
      public override Task<IEnumerable<object>> GetItemsToPlay() => Task.FromResult<IEnumerable<object>>(Array.Empty<object>());
      public override void PublishPlayEvent(IEnumerable<object> items,EventAction action) {}
      public override void PublishAddToPlaylistEvent(IEnumerable<object> items) {}
      protected override PinnedType GetPinnedType(SoundItemFilePlaylist model) => default;
    }
    [Theory]
    [InlineData(@"Z:\VPlayer-missing\Music\", "Music")]
    [InlineData(@"Z:\VPlayer-missing\tracks.m3u", "tracks.m3u")]
    [InlineData(@"\\offline-vplayer-share\music\Reggae\", "Reggae")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:/Music/Reggae/", "Reggae")]
    [InlineData(@"C:Drive relative / title", @"C:Drive relative / title")]
    [InlineData(@"Road trip / evening", @"Road trip / evening")]
    [InlineData(@"https://example.invalid/stream", @"https://example.invalid/stream")]
    [InlineData("", "GENERATED: ")]
    [InlineData(null, "GENERATED: ")]
    public void PathsAndTitlesNeedNoFilesystemOrNetworkLookup(string name,string expected)
    {
      LibraryLoadingTests.WithDispatcher(()=>
      {
        using var playlist=new Playlist(new SoundItemFilePlaylist {Id=1,Name=name});
        playlist.GetDisplayName();
        Assert.Equal(expected,playlist.DisplayName);
        return Task.CompletedTask;
      });
    }
    private static async Task UntilAsync(Func<bool> ready)
    {
      for(int i=0;i<500;i++) {if(ready()) return;await Task.Delay(10);}
      throw new TimeoutException("Display name was not published.");
    }
    [Fact]
    public void TenThousandCustomTitlesAreReadySynchronouslyWithoutBackgroundWork()
    {
      LibraryLoadingTests.WithDispatcher(()=>
      {
        for(int i=0;i<10000;i++)
        {
          var title="Road trip / evening "+i+" "+new string('W',180);
          using var playlist=new Playlist(new SoundItemFilePlaylist {Id=i+1,Name=title});
          playlist.GetDisplayName();
          Assert.Equal(title,playlist.DisplayName);
        }
        return Task.CompletedTask;
      });
    }
    [Fact]
    public void RenameAndFreshSnapshotsReplaceTheCachedDisplayName()
    {
      LibraryLoadingTests.WithDispatcher(async ()=>
      {
        for(int i=0;i<10000;i++)
        {
          var original="Original "+i+" "+new string('W',180);
          using var playlist=new Playlist(new SoundItemFilePlaylist {Id=i+1,Name=original});
          playlist.GetDisplayName();
          await UntilAsync(()=>playlist.DisplayName==original);
          var renamed="Renamed "+i+" "+new string('W',180);
          playlist.Update(new SoundItemFilePlaylist {Id=i+1,Name=renamed});
          Assert.Equal(renamed,playlist.DisplayName);
          var fresh="Fresh snapshot "+i+" "+new string('W',180);
          playlist.RefreshModel(new SoundItemFilePlaylist {Id=i+1,Name=fresh});
          Assert.Equal(fresh,playlist.DisplayName);
        }
      });
    }
  }
}