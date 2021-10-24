using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using PCloudClient.Domain;
using VCore.Standard;
using VCore.WPF.Interfaces.Managers;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud
{
  public class PCloudProvider : IPCloudProvider
  {
    private readonly ICloudService cloudService;
    private readonly IWindowManager windowManager;
    private readonly ILogger logger;

    public PCloudProvider(ICloudService cloudService, IWindowManager windowManager, ILogger logger)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region UpdateOrCreateFile

    public async Task<bool> UpdateOrCreateFile(
      long parentId,
      string[] folderNames,
      string pFileName,
      byte[] data,
      string fileExtension)
    {
      FolderInfo acutalFolder = null;
      long previousFolderId = parentId;
      int startIndex = 0;

      var parentFolders = (await cloudService.GetFoldersAsync(parentId))?.ToList();

      if(parentFolders == null)
      {
        return false;
      }

      if (parentFolders.Count == 0)
      {
        if (string.IsNullOrEmpty(folderNames[0]))
        {
          return false;
        }

        acutalFolder = await cloudService.CreateFolder(PathStringProvider.GetPathValidName(folderNames[0]), previousFolderId);

        previousFolderId = acutalFolder.id;
        startIndex++;
      }

      for (int i = startIndex; i < folderNames.Length; i++)
      {
        var acutalFolderName = folderNames[i];

        if (!string.IsNullOrEmpty(acutalFolderName))
        {
          acutalFolder = parentFolders.SingleOrDefault(x => PathStringProvider.GetNormalizedName(x.name) == PathStringProvider.GetNormalizedName(acutalFolderName));

          if (acutalFolder == null)
          {
            acutalFolder = await cloudService.CreateFolder(PathStringProvider.GetPathValidName(acutalFolderName), previousFolderId);
          }

          previousFolderId = acutalFolder.id;

          parentFolders = (await cloudService.GetFoldersAsync(previousFolderId)).ToList();
        }
      }

      if (acutalFolder != null)
      {
        var files = (await cloudService.GetFilesAsync(acutalFolder.id)).ToList();

        var fileName = pFileName + fileExtension;

        var existingFiles = files.Where(x => x.name.ToLower() == fileName.ToLower()).ToList();

        if (existingFiles.Count > 1)
        {
          Application.Current.Dispatcher.Invoke(() => { windowManager.ShowErrorPrompt($"Multiple ({existingFiles.Count}) LRC files with same name {acutalFolder.name}\\{fileName}"); });
        }
        else
        {
          var existingFile = existingFiles.SingleOrDefault();

          if (existingFile != null)
          {
            return await cloudService.WriteToFile(data, existingFile.id);
          }
          else
          {
            return await cloudService.CreateFileAndWrite(fileName, data, acutalFolder.id);
          }
        }
      }


      return false;
    }

    #endregion

    #region GetFile

    public async Task<FileInfo> GetFile(long parentId, string[] folderNames, string pFileName, string extension)
    {
      var parentFolders = (await cloudService.GetFoldersAsync(parentId))?.ToList();

      if (parentFolders != null && parentFolders.Count > 0)
      {
        FolderInfo acutalFolder = null;

        for (int i = 0; i < folderNames.Length; i++)
        {
          var acutalFolderName = folderNames[i];

          acutalFolder = parentFolders.SingleOrDefault(x => PathStringProvider.GetNormalizedName(x.name) == PathStringProvider.GetNormalizedName(acutalFolderName));

          if (acutalFolder == null)
          {
            return null;
          }

          parentFolders = (await cloudService.GetFoldersAsync(acutalFolder.id)).ToList();
        }

        if (acutalFolder != null)
        {
          var files = (await cloudService.GetFilesAsync(acutalFolder.id)).ToList();

          var fileName = pFileName + extension;

          var existingFiles = files.Where(x => x.name.ToLower() == fileName.ToLower()).ToList();

          if (existingFiles.Count > 1)
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              windowManager.ShowErrorPrompt($"Multiple ({existingFiles.Count}) LRC files with same name {acutalFolder.name}\\{fileName}");
            });
          }
          else if (existingFiles.Count > 0)
          {
            var existingSong = existingFiles.SingleOrDefault();

            return existingSong;
          }
        }
      }

      return null;
    }

    #endregion
  }
}