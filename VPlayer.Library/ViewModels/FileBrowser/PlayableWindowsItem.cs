using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.Standard.ViewModels.WindowsFile;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  //public abstract class PlayableWindowsItem<TModel, TWindowsItem> : TreeViewItemViewModel<TModel>
  //  where TModel : WindowsItem<TWindowsItem>
  //  where TWindowsItem : FileSystemInfo
  //{
  //  protected readonly IEventAggregator eventAggregator;
  //  protected readonly IStorageManager storageManager;
  //  protected readonly IViewModelsFactory viewModelsFactory;

  //  public PlayableWindowsItem(
  //    TModel model,
  //    IEventAggregator eventAggregator,
  //    IStorageManager storageManager,
  //    IViewModelsFactory viewModelsFactory) : base(model)
  //  {
  //    this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
  //    this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
  //    this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
  //  }

  //  #region PlayButton

  //  private ActionCommand playButton;

  //  public ICommand PlayButton
  //  {
  //    get
  //    {
  //      if (playButton == null)
  //      {
  //        playButton = new ActionCommand(Play);
  //      }

  //      return playButton;
  //    }
  //  }

  //  #endregion

  //  public abstract void Play();
  //}
}