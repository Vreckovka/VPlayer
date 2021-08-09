using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.ViewModels;
using VCore.WPF.Interfaces;
using VCore.WPF.Managers;
using VPlayer.Core;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.Views.FileBrowser;

namespace VPlayer.Home.ViewModels.FileBrowser
{
  public class FileBrowserViewModel : RegionViewModel<FileBrowserView>, IFilterable
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;
    private PlayableFolderViewModel root;

    #endregion

    #region Constructors

    public FileBrowserViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IWindowManager windowManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      baseDirectoryPath = GlobalSettings.FileBrowserInitialDirectory;

      if (!string.IsNullOrEmpty(baseDirectoryPath) && Directory.Exists(baseDirectoryPath))
      {
        ParentDirectory = new DirectoryInfo(baseDirectoryPath).Parent?.FullName;
      }
    }

    #endregion

    #region Properties

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

    private string parentDirectory;

    public string ParentDirectory
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

    private ObservableCollection<TreeViewItemViewModel> items = new ObservableCollection<TreeViewItemViewModel>();

    public ObservableCollection<TreeViewItemViewModel> Items
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

    #endregion

    #region Methods

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        OnBaseDirectoryPathChanged(BaseDirectoryPath);
      }
    }

    #endregion

    #region Filter

    public void Filter(string predicated)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (predicated.Length >= 3 && !string.IsNullOrEmpty(predicated) && !predicated.All(x => char.IsWhiteSpace(x)))
        {
          root.Filter(predicated);
        }
        else
        {
          root.ResetFilter();
        }
      });
    }

    #endregion

    #region OnBaseDirectoryPathChanged

    public void OnBaseDirectoryPathChanged(string newPath)
    {
      try
      {
        BaseDirectoryPath = newPath;

        if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
        {

          ParentDirectory = new DirectoryInfo(newPath).Parent?.FullName;

          root = viewModelsFactory.Create<PlayableFolderViewModel>(new DirectoryInfo(newPath));

          root.GetFolderInfo();
          root.IsExpanded = true;
          root.CanExpand = false;
          root.CanPlay = false;
          root.FolderType = FolderType.Other;
          root.IsRoot = true;

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

    #endregion
  }
}
