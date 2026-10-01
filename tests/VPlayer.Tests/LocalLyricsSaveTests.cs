using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VCore.WPF.LRC.Domain;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using Xunit;

namespace VPlayer.Tests
{
  public class LocalLyricsSaveTests : IDisposable
  {
    private readonly string root=Path.Combine(Path.GetTempPath(),"VPlayerLyricsSave-"+Guid.NewGuid());
    public LocalLyricsSaveTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    private class Provider : LocalLrcProvider
    {
      public Provider(string path) : base(path) {}
      public Task<FileInfo> Find(string title,string artist,string album)=>GetFile(title,artist,album,".lrc");
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Album")]
    [InlineData("Album: Deluxe/Live")]
    public async Task SaveAndLookupUseSameSanitizedDirectory(string album)
    {
      var provider=new Provider(root);
      var file=new LRCFile(new List<LRCLyricLine> {new LRCLyricLine {Timestamp=TimeSpan.Zero,Text="Hello"}})
        {Artist="Artist: Name/Live",Title="Song: Live",Album=album};
      Assert.True(await provider.Update(file));
      var saved=await provider.Find(file.Title,file.Artist,file.Album);
      Assert.NotNull(saved);
      Assert.Equal(file.GetString(),await File.ReadAllTextAsync(saved.FullName));
    }
    [Fact]
    public async Task MissingLyricsOrMetadataReturnsFalse()
    {
      var provider=new LocalLrcProvider(root);
      Assert.False(await provider.Update(null));
      Assert.False(await provider.Update(new LRCFile(null)));
      Assert.False(await provider.Update(new LRCFile(new List<LRCLyricLine>())));
      Assert.Empty(Directory.GetFiles(root));
    }
  }
}
