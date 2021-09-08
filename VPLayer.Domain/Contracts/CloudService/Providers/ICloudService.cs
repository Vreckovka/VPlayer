using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PCloud;
using PCloudClient.Domain;
using FileInfo = PCloudClient.Domain.FileInfo;

namespace VPLayer.Domain.Contracts.CloudService.Providers
{
  public interface ICloudService
  {
    Task<FolderInfo> GetFolderInfo(long id);
    Task<IEnumerable<FileInfo>> GetFilesAsync(long folderId);
    Task<IEnumerable<FolderInfo>> GetFoldersAsync(long folderId);
    void SaveLoginInfo(string email, string password);
    Task<bool> ExistsFolderAsync(long id);
    Task<MemoryStream> ReadFile(long id);
    Task<string> GetPublicLink(long id);
    Task<PCloudResponse<Stats>> GetFileStats(long id);
    Task<FolderInfo> CreateFolder(string name, long? parentId);
    Task<long?> CreateFile(string name, long id);
    Task<bool> WriteToFile(string sourceString, long id);
    Task<bool> CreateFileAndWrite(string name, string sourceString, long folderId);
    bool IsUserLoggedIn();
  }
}
