﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.ViewModels.WindowsFiles;
using VPLayer.Domain.Contracts.CloudService.Providers;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;

namespace VPlayer.PCloud.ViewModels
{
  public class PCloudFolderViewModel : FolderViewModel<PCloudFileViewModel>
  {
    private readonly ICloudService cloudService;

    public PCloudFolderViewModel(
      FolderInfo directoryInfo,
      IViewModelsFactory viewModelsFactory,
      ICloudService cloudService) : base(directoryInfo, viewModelsFactory)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
    }

    #region GetFiles

    public override async Task<IEnumerable<FileInfo>> GetFiles()
    {
      var files = await cloudService.GetFilesAsync(long.Parse(Model.Indentificator));

      var list = new List<FileInfo>();

      foreach (var file in files)
      {
        var fileInfo = new FileInfo(file.name, null)
        {
          Indentificator = file.id.ToString(),
          Name = file.name,
        };

        var type = fileInfo.Extension.GetFileType();

        if (type == FileType.Video || type == FileType.Sound)
        {
          fileInfo.Source = await cloudService.GetPublicLink(file.id);
        }

        list.Add(fileInfo);
      }

      return list;
    }

    #endregion

    #region GetFolders

    public override async Task<IEnumerable<FolderInfo>> GetFolders()
    {
      var folders = await cloudService.GetFoldersAsync(long.Parse(Model.Indentificator));

      return folders.Select(x => new FolderInfo()
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