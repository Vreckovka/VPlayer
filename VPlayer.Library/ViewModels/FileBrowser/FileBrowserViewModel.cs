using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Controls;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  public class FileBrowserViewModel : RegionViewModel<FileBrowserView>
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public FileBrowserViewModel(IRegionProvider regionProvider, [NotNull] IViewModelsFactory viewModelsFactory) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override string Header => "File browser";

    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    public string BaseDirectoryPath = "E:\\Torrent";

    public override void Initialize()
    {
      base.Initialize();

      var baseFolder = viewModelsFactory.Create<FolderViewModel>(new System.IO.DirectoryInfo(BaseDirectoryPath));

      baseFolder.IsExpanded = true;

      Items.Add(baseFolder);
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
  }
}
