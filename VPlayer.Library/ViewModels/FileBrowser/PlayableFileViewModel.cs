﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Library.ViewModels.FileBrowser
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
      IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

   
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
      var existing = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.DiskLocation == Model.FullName);
      var videoItems = new List<VideoItem>();

      if (existing == null)
      {
        var videoItem = new VideoItem()
        {
          Name = Model.Name,
          DiskLocation = Model.FullName
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

   
  }
}