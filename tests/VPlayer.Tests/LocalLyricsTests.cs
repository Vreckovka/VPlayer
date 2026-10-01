using System;
using System.IO;
using System.Threading.Tasks;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using Xunit;

namespace VPlayer.Tests
{
  public class LocalLyricsTests : IDisposable
  {
    private readonly string root = Path.Combine(Path.GetTempPath(), "VPlayerTests-" + Guid.NewGuid());
    private class Provider : LocalLrcProvider
    {
      public Provider(string root) : base(root) {}
      public Task<FileInfo> Find(string title, string artist, string album, string extension = ".lrc")
        => GetFile(title, artist, album, extension);
    }
    public LocalLyricsTests() => Directory.CreateDirectory(Path.Combine(root,"Artist","Album"));
    public void Dispose() => Directory.Delete(root,true);
    [Fact]
    public async Task LookupUsesExactNameAndRequestedExtension()
    {
      var folder = Path.Combine(root,"Artist","Album");
      File.WriteAllText(Path.Combine(folder,"Artist - Song.lrc"),"lyrics");
      File.WriteAllText(Path.Combine(folder,"Artist - Song.txt"),"text");
      File.WriteAllText(Path.Combine(folder,"Artist - Song remix.lrc"),"remix");
      var file = await new Provider(root).Find("Song","Artist","Album");
      Assert.Equal("Artist - Song.lrc",file.Name);
    }
    [Fact]
    public async Task NonLyricsFileIsNotReturnedAsLyrics()
    {
      File.WriteAllText(Path.Combine(root,"Artist","Album","Artist - Song.txt"),"text");
      Assert.Null(await new Provider(root).Find("Song","Artist","Album"));
    }
    [Fact]
    public async Task MissingFolderReturnsNoMatch()
      => Assert.Null(await new Provider(root).Find("Song","Missing","Album"));
  }
}
