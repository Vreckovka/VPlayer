using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using VCore;
using VCore.ItemsCollections;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Providers;
using VCore.Standard.ViewModels.TreeView;
using VCore.ViewModels;
using VCore.WPF.Interfaces;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
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

    public TFolderViewModel RootFolder { get; protected set; }

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

    #region Bookmarks

    private FolderRxObservableCollection<TFolderViewModel> bookmarks;

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



      }
    }

    #endregion

    #region LoadBookmarks

    private bool wasBookamarksLaoded;
    private Task LoadBookmarks()
    {

      return Task.Run(() =>
      {
        if (!wasBookamarksLaoded)
        {
          var allBookmarks = storageManager.GetRepository<ItemBookmark>()
            .Where(x => x.FileBrowserType == FileBrowserType);

          Application.Current.Dispatcher.Invoke(async () =>
          {
            List<TFolderViewModel> bookmarks = new List<TFolderViewModel>();

            if (RootFolder?.SubItems != null)
            {
              var ids = allBookmarks.Select(x => x.Identificator).ToList();

              var existings = AllLoadedFolders.Where(x => ids.Contains(x.Model.Indentificator));
              var nonExistings = ids.Where(x => !AllLoadedFolders.Select(y => y.Model.Indentificator).Contains(x));

              foreach (var existing in existings)
              {
                bookmarks.Add(existing);
              }

              foreach (var nonExisting in nonExistings)
              {
                var vm = await GetNewFolderViewModel(nonExisting);
                bookmarks.Add(vm);
              }

              Bookmarks = new FolderRxObservableCollection<TFolderViewModel>(bookmarks);
              Bookmarks.ForEach(x =>
              {
                x.IsBookmarked = true;
                x.RaiseNotifications(nameof(x.IsBookmarked));
              });
            }
            else
            {
              foreach (var nonExisting in allBookmarks)
              {
                bookmarks.Add(await GetNewFolderViewModel(nonExisting.Identificator));
              }
            }
          });
        }
      });
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
        wasBookamarksLaoded = false;

        if (!string.IsNullOrEmpty(newPath) && await DirectoryExists(newPath))
        {
          RootFolder = await GetNewFolderViewModel(newPath);

          if (RootFolder.Model.ParentIndentificator != null)
            ParentDirectory = await GetNewFolderViewModel(RootFolder.Model.ParentIndentificator);

          RootFolder.IsRoot = true;
          RootFolder.IsExpanded = true;
          RootFolder.CanExpand = false;
          RootFolder.FolderType = FolderType.Other;
          RootFolder.PropertyChanged += RootFolder_PropertyChanged;

          Items.Clear();
          Items.Add(RootFolder);
          IsBookmarkMenuOpened = false;
        }
      }
      catch (Exception ex)
      {
        windowManager.ShowErrorPrompt(ex);
      }
    }

    private async void RootFolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if ((e.PropertyName == nameof(FolderViewModel<PlayableFileViewModel>.WasLoaded) ||
           e.PropertyName == nameof(FolderViewModel<PlayableFileViewModel>.WasSubFoldersLoaded)) &&
          sender is TFolderViewModel viewModel &&
          viewModel.WasLoaded &&
          viewModel.WasSubFoldersLoaded)
      {
        await LoadBookmarks();
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
          var existing = storageManager.GetRepository<ItemBookmark>(context).SingleOrDefault(x => x.Identificator == bookmark.Identificator);

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
              var bookmarkExisting = Bookmarks.Single(x => x.Model.Indentificator == existing.Identificator);
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
