using System.Threading.Tasks;
using PCloudClient.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud
{
  public interface IPCloudProvider
  {
    Task<bool> UpdateOrCreateFile(
      long parentId,
      string[] folderNames,
      string pFileName,
      byte[] data,
      string fileExtension);

    Task<FileInfo> GetFile(long parentId, string[] folderNames, string pFileName, string extension);
  }
}