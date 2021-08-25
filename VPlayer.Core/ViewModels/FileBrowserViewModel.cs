using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;
using VCore;
using VCore.ItemsCollections;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.ViewModels;
using VCore.WPF.Interfaces;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.Views.FileBrowser;

namespace VPlayer.Core.ViewModels
{
  public abstract class FileBrowserViewModel<TFolderViewModel> : RegionViewModel<FileBrowserView>, IFilterable where TFolderViewModel : FolderViewModel<PlayableFileViewModel>
  {
    #region Fields

    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;
    private TFolderViewModel root;

    #endregion

    #region Constructors

    public FileBrowserViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IWindowManager windowManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      BaseDirectoryPath = GlobalSettings.FileBrowserInitialDirectory;


    }

    #endregion

    #region Properties

    public virtual Visibility FinderVisibility => Visibility.Visible;
    public override string Header => "File browser";

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

    #region BaseDirectoryPath

    private string baseDirectoryPath;

    public string BaseDirectoryPath
    {
      get { return baseDirectoryPath; }
      set
      {
        if (value != baseDirectoryPath)
        {
          baseDirectoryPath = value;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ParentDirectory

    private TFolderViewModel parentDirectory;

    public TFolderViewModel ParentDirectory
    {
      get { return parentDirectory; }
      set
      {
        if (value != parentDirectory)
        {
          parentDirectory = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Items

    private RxObservableCollection<TreeViewItemViewModel> items = new RxObservableCollection<TreeViewItemViewModel>();

    public RxObservableCollection<TreeViewItemViewModel> Items
    {
      get { return items; }
      set
      {
        if (value != items)
        {
          items = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region ChangeDirectory

    private ActionCommand<string> changeDirectory;

    public ICommand ChangeDirectory
    {
      get
      {
        if (changeDirectory == null)
        {
          changeDirectory = new ActionCommand<string>(OnBaseDirectoryPathChanged);
        }

        return changeDirectory;
      }
    }


    #endregion

    #region Refresh

    private ActionCommand refresh;

    public ICommand Refresh
    {
      get
      {
        if (refresh == null)
        {
          refresh = new ActionCommand(OnRefresh);
        }

        return refresh;
      }
    }


    private void OnRefresh()
    {
      OnBaseDirectoryPathChanged(BaseDirectoryPath);
    }

    #endregion

    #region DeleteItemCommand

    private ActionCommand<string> deleteItemCommand;

    public ICommand DeleteItemCommand
    {
      get
      {
        if (deleteItemCommand == null)
        {
          deleteItemCommand = new ActionCommand<string>(DeleteItem);
        }

        return deleteItemCommand;
      }
    }

    #endregion

    #region FilterPhrase

    private string filterPhrase;

    public string FilterPhrase
    {
      get { return filterPhrase; }
      set
      {
        if (value != filterPhrase)
        {
          filterPhrase = value;

          Filter(filterPhrase);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region OnActivation

    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        await SetUpManager();
      }
    }

    #endregion

    #region SetUpManager

    public async Task<bool> SetUpManager()
    {
      var result = await SetBaseDirectory();

      if (!result)
      {
        baseDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        result = await SetBaseDirectory();
      }

      return result;
    }

    #endregion

    #region SetBaseDirectory

    private async Task<bool> SetBaseDirectory()
    {
      var dirExists = await DirectoryExists(baseDirectoryPath);

      if (!string.IsNullOrEmpty(baseDirectoryPath) && dirExists)
      {
        ParentDirectory = await GetParentFolderViewModel(baseDirectoryPath);

        OnBaseDirectoryPathChanged(BaseDirectoryPath);

        return true;
      }

      return false;
    }

    #endregion

    #region Filter

    public void Filter(string predicated)
    {
      if (predicated.Length >= 3 && !string.IsNullOrEmpty(predicated) && !predicated.All(x => char.IsWhiteSpace(x)))
      {
        Task.Run(() =>
        {
          root.Filter(predicated);
        });

      }
      else
      {
        root.ResetFilter();
      }
    }

    #endregion

    #region OnBaseDirectoryPathChanged

    public async void OnBaseDirectoryPathChanged(string newPath)
    {
      try
      {
        BaseDirectoryPath = newPath;

        if (!string.IsNullOrEmpty(newPath) && await DirectoryExists(newPath))
        {
          root = await GetNewFolderViewModel(newPath);

          ParentDirectory = await GetNewFolderViewModel(root.Model.ParentIndentificator);

          root.IsExpanded = true;
          root.CanExpand = false;
          root.FolderType = FolderType.Other;

          Items.Clear();
          Items.Add(root);
        }
      }
      catch (Exception ex)
      {
        windowManager.ShowErrorPrompt(ex);
      }
    }

    #endregion

    #region DeleteItem

    private void DeleteItem(string path)
    {
      var result = windowManager.ShowDeletePrompt(path);

      if (result == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        try
        {

          if (Directory.Exists(path))
          {
            FileSystem.DeleteDirectory(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
          }
          else if (File.Exists(path))
          {
            FileSystem.DeleteFile(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
          }
        }
        catch (Exception ex)
        {
          windowManager.ShowErrorPrompt(ex);
        }

        OnRefresh();
      }
    }

    #endregion

    protected abstract Task<TFolderViewModel> GetNewFolderViewModel(string newPath);
    protected abstract Task<TFolderViewModel> GetParentFolderViewModel(string childIdentificator);
    protected abstract Task<bool> DirectoryExists(string newPath);


    #endregion
  }
}
