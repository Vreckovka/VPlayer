using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PCloudClient;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses.FolderStructure;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels.FileBrowser.PCloud
{
  public class PCloudFileBrowserViewModel : FileBrowserViewModel<PlayblePCloudFolderViewModel>
  {
    private readonly IPCloudService cloudService;
    private readonly IFilePlayableRegionViewModel[] players;

    public PCloudFileBrowserViewModel(
      IRegionProvider regionProvider,
      IPCloudService cloudService,
      ISettingsProvider settingsProvider,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager,
      IStorageManager storageManager,
      IPlayableRegionViewModel[] players) : base(regionProvider, viewModelsFactory, windowManager, settingsProvider, storageManager)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.players = players?.OfType<IFilePlayableRegionViewModel>().ToArray() ?? throw new ArgumentNullException(nameof(players));

      BaseDirectoryPath = settingsProvider.GetSetting(GlobalSettings.CloudBrowserInitialDirectory)?.Value;
    }

    public override Visibility FinderVisibility => Visibility.Collapsed;
    public override FileBrowserType FileBrowserType { get; } = FileBrowserType.Cloud;

    public List<PCloudClient.Domain.FolderInfo> PlayingFolders { get; private set; } = new List<PCloudClient.Domain.FolderInfo>();

    #region LocatePlayingFiles

    private ActionCommand locatePlayingFiles;

    public ICommand LocatePlayingFiles
    {
      get
      {
        if (locatePlayingFiles == null)
        {
          locatePlayingFiles = new ActionCommand(OnLocatePlayingFiles);
        }

        return locatePlayingFiles;
      }
    }

    private CancellationTokenSource ctk;
    public async void OnLocatePlayingFiles()
    {
      ctk?.Cancel();
      ctk = new CancellationTokenSource();

      foreach (var folder in Items.Generator.AllItems.OfType<PlayblePCloudFolderViewModel>())
      {
        folder.IsInPlaylist = false;

        foreach (var folder2 in GetRecursiveFolders(folder))
        {
          folder2.IsInPlaylist = false;
        };
      }

      PlayingFolders.Clear();

      await Task.Run(async () =>
      {
        var playlist = players.OfType<IMusicPlayerViewModel>().FirstOrDefault()?.PCloudIds.ToList();

        for (int i = 0; i < playlist.Count; i++)
        {
          var item = playlist[i];

          var file = await cloudService.GetFileStats(item);

          if (file.metadata != null)
          {
            var folderId = long.Parse(file.metadata.parentfolderid);

            for (int j = 0; j < 3; j++)
            {
              if (!PlayingFolders.Any(x => x.id == folderId))
              {
                if (ctk.IsCancellationRequested)
                {
                  return;
                }

                var folderInfo = await cloudService.GetFolderInfo(folderId);

                PlayingFolders.Add(folderInfo);

                if (folderInfo.children != null)
                {
                  foreach (var child in folderInfo.children)
                    playlist.Remove(child.id);
                }

                if (folderInfo != null)
                {
                  folderId = folderInfo.parentFolderId;
                }
              }
            }
          }

          if (i % 10 == 0)
            Application.Current.Dispatcher.Invoke(() =>
            {
              RaisePropertyChanged(nameof(PlayingFolders));
            });
        }
      });

      var folders = new List<PlayblePCloudFolderViewModel>();

      foreach (var folder in Items.Generator.AllItems.OfType<PlayblePCloudFolderViewModel>())
      {
        folders.AddRange(GetRecursiveFolders(folder));
      }

      foreach (var playlistFolder in PlayingFolders)
      {
        var existingFolder = folders.SingleOrDefault(x => x.Model.Indentificator == playlistFolder.id.ToString());

        if (existingFolder != null)
        {
          existingFolder.IsInPlaylist = true;
        }
      }

      RaisePropertyChanged(nameof(PlayingFolders));
    }

    private IEnumerable<PlayblePCloudFolderViewModel> GetRecursiveFolders(PlayblePCloudFolderViewModel playblePCloudFolderViewModel)
    {
      var list = playblePCloudFolderViewModel.SubItems.ViewModels.OfType<PlayblePCloudFolderViewModel>().ToList();

      foreach (var item in playblePCloudFolderViewModel.SubItems.ViewModels.OfType<PlayblePCloudFolderViewModel>())
      {
        list.AddRange(GetRecursiveFolders(item));
      }

      return list;
    }

    #endregion

    public override Task<bool> SetUpManager()
    {
      return base.SetUpManager();
    }

    protected override void OnDeleteItem(string indentificator)
    {
      throw new NotImplementedException();
    }

    protected override async Task<PlayblePCloudFolderViewModel> GetNewFolderViewModel(string newPath)
    {
      var dir = await cloudService.GetFolderInfo(long.Parse(newPath));

      var info = new FolderInfo()
      {
        Indentificator = dir.id.ToString(),
        Name = dir.name,
        ParentIndentificator = dir.parentFolderId.ToString()
      };

      var folderViewModel = viewModelsFactory.Create<PCloudFolderViewModel>(info);

      return viewModelsFactory.Create<PlayblePCloudFolderViewModel>(folderViewModel);
    }

    protected override async Task<PlayblePCloudFolderViewModel> GetParentFolderViewModel(string childIdentificator)
    {
      var parent = await cloudService.GetFolderInfo(long.Parse(childIdentificator));

      if (parent != null)
      {
        var parentId = parent.parentFolderId.ToString();

        return await GetNewFolderViewModel(parentId);
      }

      return null;
    }

    protected override Task<bool> DirectoryExists(string newPath)
    {
      return cloudService.ExistsFolderAsync(long.Parse(newPath));
    }
  }
}