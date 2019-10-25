using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;

namespace VPlayer.UnitTest
{
  [TestClass]
  public class AudioInfoDownloaderTests
  {
    [TestMethod]
    [Timeout(5000)]
    public async void UpdateAlbum_NotNullAlbum()
    {
      //Arrange
      var audioInfoDownloader = new AudioInfoDownloader();

      Artist artist = new Artist()
      {
        Name = "Slipknot"
      };

      var album1 = new Album()
      {
        Name = ".5 The Gray Chapter (Deluxe version)",
        Artist = artist
      };

      //Act
      var album = await audioInfoDownloader.UpdateAlbum(album1);


      //Assert
      Assert.IsNotNull(album);
    }
  }
}
