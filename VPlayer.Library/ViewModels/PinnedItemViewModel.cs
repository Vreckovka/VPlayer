using System;
using System.IO;
using System.Windows.Input;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.ViewModels.FileBrowser.PCloud;
using VPlayer.Home.ViewModels.FileBrowser;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;

namespace VPlayer.Home.ViewModels
{
  public class PinnedItemViewModel : ViewModel<PinnedItem>
  {
    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;

    public PinnedItemViewModel(PinnedItem model, IStorageManager storageManager, IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }


    #region ItemObject

    private object itemObject;

    public object ItemObject
    {
      get { return itemObject; }
      set
      {
        if (value != itemObject)
        {
          itemObject = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    #region DisplayText

    public string DisplayText
    {
      get
      {
        if (Directory.Exists(Model.Description))
          return new DirectoryInfo(Model.Description).Name;

        if (File.Exists(Model.Description))
          return new System.IO.FileInfo(Model.Description).Name;

        return Model.Description;
      }
    }

    #endregion


    #region DeleteItem

    private ActionCommand deleteItem;

    public ICommand DeleteItem
    {
      get
      {
        if (deleteItem == null)
        {
          deleteItem = new ActionCommand(OnDeleteItem);
        }

        return deleteItem;
      }
    }

    public void OnDeleteItem()
    {
      var foundItem = storageManager.RemovePinnedItem(Model);
    }

    #endregion

    #region Play

    private ActionCommand play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand(OnPlay);
        }

        return play;
      }
    }

    private WindowsFolderViewModel windowsFolderVm;
    private PCloudFolderViewModel pCloudFolderViewModel;

    // private PCloudFolderViewModel pCloudFolderViewModel;

    public void OnPlay()
    {
      if (Model.PinnedType == PinnedType.SoundFolder || Model.PinnedType == PinnedType.VideoFolder)
      {
        if (!int.TryParse(Model.Description, out var pcloudId))
        {
          if (windowsFolderVm == null)
          {
            windowsFolderVm = new WindowsFolderViewModel(new FolderInfo()
            {
              Indentificator = Model.Description,
            }, viewModelsFactory);
          }

          var folderVm = viewModelsFactory.Create<PlayableWindowsFileFolderViewModel>(windowsFolderVm);

          folderVm.Play(Core.Events.EventAction.Play);
        }
        else
        {
          if (pCloudFolderViewModel == null)
          {
            pCloudFolderViewModel = viewModelsFactory.Create<PCloudFolderViewModel>(new FolderInfo()
            {
              Indentificator = Model.Description,
            });
          }

          var folderVm = viewModelsFactory.Create<PlayblePCloudFolderViewModel>(pCloudFolderViewModel);

          folderVm.Play(Core.Events.EventAction.Play);
        }

      }
      else if (Model.PinnedType == PinnedType.SoundFile || Model.PinnedType == PinnedType.VideoFile)
      {
        if (!Model.Description.ToLower().Contains("http"))
        {
          var fileViewModel = viewModelsFactory.Create<PlayableFileViewModel>(new FileInfo()
          {
            Indentificator = Model.Description,
          });

          fileViewModel.Play(Core.Events.EventAction.Play);
        }
        else
        {
          //TODO
        }
      }

    }

    #endregion
  }
}