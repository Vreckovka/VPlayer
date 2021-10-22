using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;

namespace VPlayer.Core.FileBrowser
{
  public class PlayableFileViewModel : FileViewModel
  {
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;

    public PlayableFileViewModel(
      FileInfo model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IWindowManager windowManager,
      IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      if (FileType == FileType.Video || FileType == FileType.Sound)
      {
        CanPlay = true;
      }
    }

    #region CanPlay

    private bool canPlay;

    public bool CanPlay
    {
      get { return canPlay; }
      set
      {
        if (value != canPlay)
        {
          canPlay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

    #region PlayButton

    private ActionCommand playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand(Play);
        }

        return playButton;
      }
    }

    #endregion

    #endregion

    #region Play

    public void Play()
    {
      if (FileType == FileType.Video)
      {
        PlayVideo();
      }
    }

    #endregion

    #region PlayVideo

    private void PlayVideo()
    {
      var existing = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.Source == Model.Indentificator);
      var videoItems = new List<VideoItem>();

      if (existing == null)
      {
        var videoItem = new VideoItem()
        {
          Name = Model.Name,
          Source = Model.Indentificator
        };

        storageManager.StoreEntity<VideoItem>(videoItem, out var stored);

        videoItems.Add(stored);
      }
      else
      {
        videoItems.Add(existing);
      }

      var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)), EventAction.Play, this);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
    }

    #endregion


    public override void OnOpenContainingFolder()
    {
      if (!string.IsNullOrEmpty(Model.Indentificator))
      {
        var folder = Model.Indentificator;

        if (!Directory.Exists(Model.Indentificator))
        {
          folder = System.IO.Path.GetDirectoryName(Model.Indentificator);
        }

        if (!string.IsNullOrEmpty(folder))
        {
          Process.Start(new System.Diagnostics.ProcessStartInfo()
          {
            FileName = folder,
            UseShellExecute = true,
            Verb = "open"
          });
        }
      }
    }
  }
}