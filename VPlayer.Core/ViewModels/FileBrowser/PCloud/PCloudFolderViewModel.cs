using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCloudClient;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels.WindowsFiles;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;
using FolderInfo = VCore.WPF.ViewModels.WindowsFiles.FolderInfo;

namespace VPlayer.Core.ViewModels.FileBrowser.PCloud
{
  public class PCloudFolderViewModel : FolderViewModel<PCloudFileViewModel>
  {
    private readonly IPCloudService cloudService;

    public PCloudFolderViewModel(
      FolderInfo directoryInfo,
      IViewModelsFactory viewModelsFactory,
      IPCloudService cloudService) : base(directoryInfo, viewModelsFactory)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
    }

    public override int MaxAutomaticLoadLevel => 0;

    #region GetFiles

    public override async Task<IEnumerable<FileInfo>> GetFiles(bool recursive = false)
    {
      var files = await cloudService.GetFilesAsync(long.Parse(Model.Indentificator), recursive);

      var list = new List<FileInfo>();

      foreach (var file in files)
      {
        var fileInfo = new FileInfo(file.name, null)
        {
          Indentificator = file.id.ToString(),
          Name = file.name,
          Length = file.length,
        };

        list.Add(fileInfo);
      }

      return list;
    }

    #endregion

    #region GetFolders

    public override async Task<IEnumerable<FolderInfo>> GetFolders()
    {
      var folders = await cloudService.GetFoldersAsync(long.Parse(Model.Indentificator));

      return folders?.Select(x => new FolderInfo()
      {
        Indentificator = x.id.ToString(),
        Name = x.name,
        ParentIndentificator = x.parentFolderId.ToString()
      });
    }

    #endregion

    #region OnOpenContainingFolder

    public override void OnOpenContainingFolder()
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
