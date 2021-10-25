using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PCloudClient;
using VCore.WPF.Interfaces.Managers;


namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud.Images
{
  public interface IPCloudAlbumCoverProvider
  {
    Task<bool> UpdateOrCreateAlbumCover(string albumName, string artistName, byte[] data);
    Task<byte[]> GetAlbumCover(string artistName, string albumName);
  }

  public class PCloudAlbumCoverProvider : IPCloudAlbumCoverProvider
  {
    private readonly IPCloudService cloudService;
    private readonly IPCloudProvider pCloudProvider;

    private string fileExtension = ".jpg";
    private long lyricsFolderId = 1457701143;

    public PCloudAlbumCoverProvider(IPCloudService cloudService, IWindowManager windowManager, IPCloudProvider pCloudProvider)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.pCloudProvider = pCloudProvider ?? throw new ArgumentNullException(nameof(pCloudProvider));
    }

    #region UpdateOrCreateFile

    public Task<bool> UpdateOrCreateAlbumCover(string artistName, string albumName, byte[] data)
    {
      var folders = new string[] { artistName, albumName };
      var fileName = GetFileName();

      return pCloudProvider.UpdateOrCreateFile(lyricsFolderId, folders, fileName, data, fileExtension);
    }

    #endregion

    #region GetFile

    public async Task<byte[]> GetAlbumCover(string artistName, string albumName)
    {
      var folders = new string[] { artistName, albumName };
      var fileName = GetFileName();

      var fileId = (await pCloudProvider.GetFile(lyricsFolderId, folders, fileName, fileExtension))?.id;

      if (fileId != null)
      {
        var stream = await cloudService.ReadFile(fileId.Value);

        return stream.ToArray();
      }

      return null;
    }

    #endregion

    private string GetFileName()
    {
      return "cover";
    }
  }
}
