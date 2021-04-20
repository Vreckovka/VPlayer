using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VCore;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.ViewModels;
using VCore.WPF.Interfaces;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  public class FileBrowserViewModel : RegionViewModel<FileBrowserView>, IFilterable
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public FileBrowserViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override string Header => "File browser";

    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    public string BaseDirectoryPath = "E:\\Torrent";

    private PlayableFolderViewModel root;
    public override void Initialize()
    {
      base.Initialize();

      root = viewModelsFactory.Create<PlayableFolderViewModel>(new DirectoryInfo(BaseDirectoryPath));

      root.GetFolderInfo();
      root.IsExpanded = true;
      root.CanExpand = false;
      root.CanPlay = false;
      root.FolderType = FolderType.Other;
      root.IsRoot = true;

      Items.Add(root);
    }

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
  }
}
