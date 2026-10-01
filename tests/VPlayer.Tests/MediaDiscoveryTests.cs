using System;
using System.IO;
using System.Linq;
using Moq;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.Core.Managers.Status;
using Xunit;

namespace VPlayer.Tests
{
  public class MediaDiscoveryTests : IDisposable
  {
    private readonly string root = Path.Combine(Path.GetTempPath(),"VPlayerDiscovery-" + Guid.NewGuid());
    public MediaDiscoveryTests()
    {
      Directory.CreateDirectory(Path.Combine(root,"Child"));
      foreach(var file in new[] {"movie.MKV","video.mp4","movie.avi","ignore.txt","sound.mp3","sound.flac","sound.m4a","sound.mp4a","sound.ogg","sound.wav","Child/child.MP4"})
        File.WriteAllText(Path.Combine(root,file),"");
    }
    public void Dispose() => Directory.Delete(root,true);
    private DataLoader Loader() => new DataLoader(new Mock<IStatusManager>().Object);
    [Fact]
    public void VideoScanFiltersExtensionsAndHonorsRecursion()
    {
      Assert.Equal(3,Loader().LoadData(DataType.Video,root,false).Count);
      var files = Loader().LoadData(DataType.Video,root,true);
      Assert.Equal(4,files.Count);
      Assert.Equal("child.MP4",files[0].Name);
      Assert.DoesNotContain(files,x=>x.Extension==".txt");
    }
    [Fact]
    public void AudioScanSupportsAllConfiguredFormats()
      => Assert.Equal(6,Loader().LoadData(DataType.Audio,root,false).Count);
    [Fact]
    public void FilePathIsNotScannedAsDirectory()
      => Assert.Empty(Loader().LoadData(DataType.Video,Path.Combine(root,"video.mp4"),true));
    [Fact]
    public void DiscoveryAcceptsOverlappingPatternsWithoutDuplicateFiles()
      => Assert.Single(MediaFileDiscovery.EnumerateFiles(root,new[] {"*.mkv","*.MKV"},false));
  }
}
