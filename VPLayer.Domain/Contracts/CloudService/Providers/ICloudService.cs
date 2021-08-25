using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PCloud;
using PCloudClient.Domain;

namespace VPLayer.Domain.Contracts.CloudService.Providers
{
  public interface ICloudService
  {
    Task<FolderInfo> GetFolderInfo(long id);
    Task<IEnumerable<FileInfo>> GetFilesAsync(long folderId);
    Task<IEnumerable<FolderInfo>> GetFoldersAsync(long folderId);
    void SaveLoginInfo(string email, string password);
    Task<bool> ExistsFolderAsync(long id);
    Task<string> GetPublicLink(long id);
    Task<PCloudResponse<Stats>> GetFileStats(long id);
    bool IsUserLoggedIn();
  }
}
