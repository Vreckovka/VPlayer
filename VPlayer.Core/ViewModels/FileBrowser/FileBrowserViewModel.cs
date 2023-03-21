using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Providers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF;
using VCore.WPF.Interfaces;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.FolderStructure;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.Views.FileBrowser;

namespace VPlayer.Core.ViewModels
{
  public class FolderRxObservableCollection<TFolderViewModel> : RxObservableCollection<TFolderViewModel>
    where TFolderViewModel : FolderViewModel<PlayableFileViewModel>
  {
    public FolderRxObservableCollection()
    {

    }

    public FolderRxObservableCollection(IEnumerable<TFolderViewModel> items) : base(items)
    {

    }

    protected override string KeySelector(TFolderViewModel other)
    {
      return other.Model.Name;
    }
  }

  public abstract class FileBrowserViewModel<TFolderViewModel> : RegionViewModel<FileBrowserView>, IFilterable
    where TFolderViewModel : FolderViewModel<PlayableFileViewModel>
  {
    #region Fields

    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;
    private readonly IStorageManager storageManager;
    protected readonly ISettingsProvider settingsProvider;


    #endregion

    #region Constructors

    public FileBrowserViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager,
      ISettingsProvider settingsProvider,
      IStorageManager storageManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    #endregion

    #region Properties

    #region RootFolder

    private TFolderViewModel rootFolder;

    public TFolderViewModel RootFolder
    {
      get { return rootFolder; }
      protected set
      {
        if (value != rootFolder)
        {
          rootFolder = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    public virtual Visibility FinderVisibility => Visibility.Visible;
    public override string Header => "File browser";

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public abstract FileBrowserType FileBrowserType { get; }

    #region IsBookmarkMenuOpened

    private bool isBookmarkMenuOpened;

    public bool IsBookmarkMenuOpened
    {
      get { return isBookmarkMenuOpened; }
      set
      {
        if (value != isBookmarkMenuOpened)
        {
          isBookmarkMenuOpened = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    private VirtualList<TreeViewItemViewModel> items;

    public VirtualList<TreeViewItemViewModel> Items
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

    #region Bookmarks

    private FolderRxObservableCollection<TFolderViewModel> bookmarks = new FolderRxObservableCollection<TFolderViewModel>();

    public FolderRxObservableCollection<TFolderViewModel> Bookmarks
    {
      get { return bookmarks; }
      set
      {
        if (value != bookmarks)
        {
          bookmarks = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AllLoadedFolders

    public IEnumerable<TFolderViewModel> AllLoadedFolders
    {
      get
      {
        List<TFolderViewModel> allFoldersList = new List<TFolderViewModel>();

        if (RootFolder != null)
        {
          var allFolders = RootFolder.SubItems.ViewModels.OfType<TFolderViewModel>().SelectManyRecursive(x => x.SubItems.ViewModels.OfType<TFolderViewModel>());
          allFoldersList = allFolders.Concat(RootFolder.SubItems.ViewModels.OfType<TFolderViewModel>()).ToList();

          allFoldersList.Add(RootFolder);

        }

        return allFoldersList;
      }
    }

    #endregion

    #region AllLoadedItems

    public IEnumerable<TreeViewItemViewModel> AllLoadedItems
    {
      get
      {
        List<TreeViewItemViewModel> allFoldersList = new List<TreeViewItemViewModel>();

        if (RootFolder != null)
        {
          var allFolders = RootFolder.SubItems.ViewModels.SelectManyRecursive(x => x.SubItems.ViewModels);
          allFoldersList = allFolders.Concat(RootFolder.SubItems.ViewModels).ToList();

          allFoldersList.Add(RootFolder);

        }

        return allFoldersList;
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

    #region AddBookMark

    private ActionCommand<TFolderViewModel> addBookmark;

    public ICommand AddBookmark
    {
      get
      {
        if (addBookmark == null)
        {
          addBookmark = new ActionCommand<TFolderViewModel>(OnAddBookmark);
        }

        return addBookmark;
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
        if (string.IsNullOrEmpty("baseDirectory"))
        {
          BaseDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        await SetUpManager();
        await LoadBookmarks();
      }
    }

    #endregion

    #region LoadBookmarks

    private bool wasBookamarksLaoded;
    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    private async Task LoadBookmarks()
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        await Task.Run(() =>
        {
          if (!wasBookamarksLaoded)
          {
            var allBookmarks = storageManager.GetTempRepository<ItemBookmark>()
              .Where(x => x.FileBrowserType == FileBrowserType);

            VSynchronizationContext.PostOnUIThread(async () =>
            {
              List<TFolderViewModel> bookmarks = new List<TFolderViewModel>();

              var ids = allBookmarks.Select(x => x.Identificator).ToList();
              var list = AllLoadedFolders.ToList();

              if (!list.Contains(RootFolder))
                list.Add(RootFolder);

              var existings = list.Where(x => ids.Contains(x.Model.Indentificator));
              var nonExistings = ids.Where(x => !list.Select(y => y.Model.Indentificator).Contains(x));

              foreach (var existing in existings.Where(x => !Bookmarks.Any(y => y.Path == x.Path)))
              {
                bookmarks.Add(existing);
              }

              foreach (var nonExisting in nonExistings.Where(x => !Bookmarks.Any(y => y.Path == x)))
              {
                var vm = await GetNewFolderViewModel(nonExisting);
                bookmarks.Add(vm);
              }

              Bookmarks.AddRange(bookmarks);

              Bookmarks.ForEach(x =>
              {
                x.IsBookmarked = true;
                x.RaiseNotifications(nameof(x.IsBookmarked));
              });
            });
          }
        });
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region SetUpManager

    public virtual async Task<bool> SetUpManager()
    {
      var result = await SetBaseDirectory();

      return result;
    }

    #endregion

    #region SetBaseDirectory

    protected async Task<bool> SetBaseDirectory()
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
          RootFolder.Filter(predicated);
        });

      }
      else
      {
        RootFolder.ResetFilter();
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
          RootFolder = await GetNewFolderViewModel(newPath);

          if (RootFolder.Model.ParentIndentificator != null)
            ParentDirectory = await GetNewFolderViewModel(RootFolder.Model.ParentIndentificator);

          RootFolder.IsRoot = true;
          RootFolder.IsExpanded = true;
          RootFolder.CanExpand = false;

          if (Bookmarks.Any(x => x.Model.Indentificator == RootFolder.Model.Indentificator))
          {
            RootFolder.IsBookmarked = true;
          }

          await RootFolder.LoadFolder();

          var generator = new ItemsGenerator<TreeViewItemViewModel>(RootFolder.SubItems.ViewModels.Select(x => x), 30);
          Items = new VirtualList<TreeViewItemViewModel>(generator);

          IsBookmarkMenuOpened = false;

          //var pinnedItems = await Task.Run(() =>
          //{
          //  return storageManager.GetTempRepository<PinnedItem>().Where(x => x.PinnedType == PinnedType.SoundFile ||
          //                                                                   x.PinnedType == PinnedType.SoundFolder ||
          //                                                                   x.PinnedType == PinnedType.VideoFile ||
          //                                                                   x.PinnedType == PinnedType.VideoFolder 
          //                                                                   ).ToList();
          //});
        }
      }
      catch (Exception ex)
      {
        windowManager.ShowErrorPrompt(ex);
      }
    }

    #endregion

    #region DeleteItem

    private async void DeleteItem(string path)
    {
      var result = windowManager.ShowDeletePrompt(path);

      if (result == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        try
        {
          OnDeleteItem(path);
        }
        catch (Exception ex)
        {
          windowManager.ShowErrorPrompt(ex);
        }
      }
    }

    #endregion

    #region OnAddBookmark

    private async void OnAddBookmark(TFolderViewModel folder)
    {
      if (folder != null)
      {
        var bookmark = new ItemBookmark()
        {
          FileBrowserType = FileBrowserType,
          Identificator = folder.Model.Indentificator,
          Path = folder.Path
        };

        using (var context = new AudioStorage.AudioDatabase.AudioDatabaseContext())
        {
          var existing = storageManager.GetTempRepository<ItemBookmark>(context).FirstOrDefault(x => x.Identificator == bookmark.Identificator);

          if (existing == null)
          {
            context.Add(bookmark);
          }
          else
          {
            context.Remove(existing);
          }

          var result = context.SaveChanges() == 1;

          if (result)
          {
            if (existing != null)
            {
              var bookmarkExisting = Bookmarks.First(x => x.Model.Indentificator == existing.Identificator);
              bookmarkExisting.IsBookmarked = false;

              Bookmarks.Remove(bookmarkExisting);

              storageManager.PublishItemChanged(existing, Changed.Removed);
            }
            else
            {
              var bookMarkFolder = AllLoadedFolders.SingleOrDefault(x => x.Model.Indentificator == bookmark.Identificator);

              if (bookMarkFolder == null)
              {
                bookMarkFolder = await GetNewFolderViewModel(bookmark.Path);
              }

              bookMarkFolder.IsBookmarked = true;
              Bookmarks.Add(bookMarkFolder);
              storageManager.PublishItemChanged(bookmark, Changed.Added);
            }
          }
        }
      }
    }

    #endregion

    protected abstract void OnDeleteItem(string indentificator);
    protected abstract Task<TFolderViewModel> GetNewFolderViewModel(string newPath);
    protected abstract Task<TFolderViewModel> GetParentFolderViewModel(string childIdentificator);
    protected abstract Task<bool> DirectoryExists(string newPath);


    #endregion
  }
}
