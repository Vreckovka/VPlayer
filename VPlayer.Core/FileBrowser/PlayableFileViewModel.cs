using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VFfmpeg;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;
using VCore.Standard.Helpers;
using VCore.WPF.Helpers;
using VPlayer.Core.ViewModels.SoundItems;
using Size = System.Drawing.Size;

namespace VPlayer.Core.FileBrowser
{
  public class ThumbnailViewModel
  {
    public byte[] ImageData { get; set; }

    public TimeSpan Time { get; set; }
  }

  public class PlayableFileViewModel : FileViewModel
  {
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private readonly IViewModelsFactory viewModelsFactory;
    ImageConverter converter = new ImageConverter();

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

    #region Properties

    public ObservableCollection<ThumbnailViewModel> Thumbnails { get; } = new ObservableCollection<ThumbnailViewModel>();

    #region ThumbnailsLoading

    private bool thumbnailsLoading;

    public bool ThumbnailsLoading
    {
      get { return thumbnailsLoading; }
      set
      {
        if (value != thumbnailsLoading)
        {
          thumbnailsLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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


    #region VideoItemInPlaylistViewModel

    private VideoItemInPlaylistViewModel videoItemInPlaylistViewModel;

    public VideoItemInPlaylistViewModel VideoItemInPlaylistViewModel
    {
      get { return videoItemInPlaylistViewModel; }
      set
      {
        if (value != videoItemInPlaylistViewModel)
        {
          videoItemInPlaylistViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsInPlaylist

    private bool isInPlaylist;

    public bool IsInPlaylist
    {
      get { return isInPlaylist; }
      set
      {
        if (value != isInPlaylist)
        {
          isInPlaylist = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region CreateThumbnails

    private ActionCommand createThumbnails;

    public ICommand CreateThumbnails
    {
      get
      {
        if (createThumbnails == null)
        {
          createThumbnails = new ActionCommand(async () => await CreateImages());
        }

        return createThumbnails;
      }
    }

    #endregion

    public byte[] ImageToByte(Image img)
    {
      return (byte[])converter.ConvertTo(img, typeof(byte[]));
    }

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public async Task CreateImages()
    {
      if (semaphoreSlim.CurrentCount == 0)
        return;

      try
      {
        await semaphoreSlim.WaitAsync();

        if (!Thumbnails.Any())
          await TryCreateThumbnails();
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region Methods

    #region Play

    public void Play()
    {
      if (FileType == FileType.Video)
      {
        PlayVideo();
      }
      else if (FileType == FileType.Sound)
      {
        PlaySound();
      }
    }

    #endregion

    #region PlayVideo

    private void PlayVideo()
    {
      VideoItem videoItem = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.Source == Model.Indentificator);

      if (videoItem == null)
      {
        var pVideoItem = new VideoItem()
        {
          Name = Model.Name,
          Source = Model.Indentificator
        };

        storageManager.StoreEntity<VideoItem>(pVideoItem, out videoItem);
      }

      if (videoItem == null)
        return;

      var vms = viewModelsFactory.Create<VideoItemInPlaylistViewModel>(videoItem);

      vms.ObservePropertyChange(x => x.IsInPlaylist).ObserveOnDispatcher().Subscribe((x) =>
      {
        IsInPlaylist = x;
      });

      var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(vms.AsList(), EventAction.Play, this);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
    }

    #endregion

    #region PlaySound

    private void PlaySound()
    {
      SoundItem soundItem = storageManager.GetRepository<SoundItem>().Include(x => x.FileInfo).SingleOrDefault(x => x.FileInfo.Source == Model.Indentificator);

      if (soundItem == null)
      {
        var pSoundItem = new SoundItem()
        {
          Name = Model.Name,
        };

        SoundFileInfo fileInfo = new SoundFileInfo()
        {
          Name = Model.Name,
          Source = Model.Indentificator
        };

        pSoundItem.FileInfo = fileInfo;

        storageManager.StoreEntity<SoundItem>(pSoundItem, out soundItem);
      }

      if (soundItem == null)
        return;

      var vms = viewModelsFactory.Create<SoundItemInPlaylistViewModel>(soundItem);

      vms.ObservePropertyChange(x => x.IsInPlaylist).ObserveOnDispatcher().Subscribe((x) =>
      {
        IsInPlaylist = x;
      });

      var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(vms.AsList(), EventAction.Play, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(data);
    }

    #endregion

    #region OnOpenContainingFolder

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

    #endregion

    #region TryCreateThumbnails

    private async Task TryCreateThumbnails()
    {
      try
      {
        if (FileType == FileType.Video)
        {
          var thmbs = new List<ThumbnailViewModel>();
          Thumbnails.Clear();
          ThumbnailsLoading = true;

          await Task.Run(async () =>
          {
            var mediaInfo = await FFProbe.AnalyseAsync(Model.Source);
            double totalSeconds = mediaInfo.Duration.TotalSeconds * 0.9;
            int numberOfScreenshots = 5;
            int screenInterval = (int)totalSeconds / numberOfScreenshots;


            var width = mediaInfo.VideoStreams[0].Width;
            var height = mediaInfo.VideoStreams[0].Height;

            double desiredWidth = 480.0;

            var sizeCoef = Math.Floor(width / desiredWidth) > 0 ? Math.Floor(width / desiredWidth) : 1;

            for (int i = screenInterval; i < (int)mediaInfo.Duration.TotalSeconds; i += screenInterval)
            {
              var img = FFMpeg.Snapshot(Model.Source, new Size((int)(width / sizeCoef), (int)(height / sizeCoef)), TimeSpan.FromSeconds(i));

              thmbs.Add(new ThumbnailViewModel()
              {
                ImageData = ImageToByte(img)
              });
            }
          });

          Thumbnails.AddRange(thmbs);
        }
      }
      finally
      {
        ThumbnailsLoading = false;
      }
    }


    #endregion

    #endregion


  }
}