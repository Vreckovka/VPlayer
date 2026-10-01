using System;
using ChromeDriverScrapper;
using Logger;
using Moq;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud.Images;
using VPlayer.Core.Managers.Status;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using Xunit;

namespace VPlayer.Tests
{
  public class MetadataTests
  {
    private class Downloader : AudioInfoDownloader
    {
      public AudioInfo WindowsInfo;
      public AudioInfo FingerprintInfo;
      public int FingerprintCalls;
      public Downloader() : base(new Mock<ILogger>().Object,new Mock<IStatusManager>().Object,
        new Mock<IPCloudAlbumCoverProvider>().Object,
        new MusixMatchLyricsProvider(new Mock<IChromeDriverProvider>().Object,new Mock<ILogger>().Object)) {}
      public override AudioInfo GetAudioInfoByWindowsAsync(string path)=>WindowsInfo;
      public override AudioInfo GetAudioInfoByFingerPrint(string path,AudioInfo info=null)
      {
        FingerprintCalls++;
        return FingerprintInfo;
      }
    }
    [Fact]
    public async System.Threading.Tasks.Task UnsupportedMetadataUpdateFailsWithoutRecursing()
    {
      var downloader=new Downloader();
      await Assert.ThrowsAsync<ArgumentNullException>(()=>downloader.UpdateItem(null));
      await Assert.ThrowsAsync<ArgumentException>(()=>downloader.UpdateItem(new object()));
    }
    [Theory]
    [InlineData(null,null)]
    [InlineData("","")]
    [InlineData(" "," ")]
    [InlineData(null,"")]
    public void MissingArtistAndAlbumUseFingerprintFallback(string artist,string album)
    {
      var fallback=new AudioInfo {Artist="Recognized artist",Album="Recognized album"};
      var downloader=new Downloader {WindowsInfo=new AudioInfo {Artist=artist,Album=album},FingerprintInfo=fallback};
      Assert.Same(fallback,downloader.GetAudioInfo("track.mp3"));
      Assert.Equal(1,downloader.FingerprintCalls);
    }
    [Fact]
    public void ExistingMetadataAvoidsExpensiveFingerprintLookup()
    {
      var info=new AudioInfo {Artist="Artist",Album="Album"};
      var downloader=new Downloader {WindowsInfo=info};
      Assert.Same(info,downloader.GetAudioInfo("track.mp3"));
      Assert.Equal(0,downloader.FingerprintCalls);
    }
    [Fact]
    public void UnrecognizedFingerprintPreservesOriginalFileInfo()
    {
      var info=new AudioInfo {Title="File name"};
      var downloader=new Downloader {WindowsInfo=info};
      Assert.Same(info,downloader.GetAudioInfo("track.mp3"));
      Assert.Equal(1,downloader.FingerprintCalls);
    }
  }
}




