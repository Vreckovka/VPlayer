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
    private readonly IVFfmpegProvider iVFfmpegProvider;
    ImageConverter converter = new ImageConverter();

    public PlayableFileViewModel(
      FileInfo model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IWindowManager windowManager,
      IViewModelsFactory viewModelsFactory,
      IVFfmpegProvider iVFfmpegProvider) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iVFfmpegProvider = iVFfmpegProvider ?? throw new ArgumentNullException(nameof(iVFfmpegProvider));

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

    private RxObservableCollection<VideoItemInPlaylistViewModel> videoItemInPlaylistViewModels = new RxObservableCollection<VideoItemInPlaylistViewModel>();

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

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;
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

    private async Task TryCreateThumbnails()
    {
      var thmbs = new List<ThumbnailViewModel>();
      Thumbnails.Clear();

      await Task.Run(() =>
      {
        using (var video = new VideoCapture(Model.FullName))
        {
          var framesC = video.Get(CapProp.FrameCount) * 0.9;
          video.Set(CapProp.FrameHeight, 640);
          video.Set(CapProp.FrameWidth, 360);
          video.Set(CapProp.Fps, 50);
          video.Set(CapProp.HwAcceleration, 1);

          int numberOfScreenshots = 5;
          int screenInterval = (int)framesC / numberOfScreenshots;

          for (int i = screenInterval; i < framesC + screenInterval; i += screenInterval)
          {
            video.Set(CapProp.PosFrames, i);
            var img = video.QuerySmallFrame();

            if (img != null)
            {
              thmbs.Add(new ThumbnailViewModel()
              {
                ImageData = ImageToByte(img.ToBitmap())
              });
            }
          }
        }
      });

      Thumbnails.AddRange(thmbs);
    }

    #region Methods

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

      var vms = videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)).ToList();

      videoItemInPlaylistViewModels.ItemUpdated.Subscribe((x) =>
      {
        var vm = ((VideoItemInPlaylistViewModel)x.Sender);
        if (vm.Model.Source == Model.Indentificator)
        {
          IsPlaying = vm.IsPlaying;
        }
      });

      videoItemInPlaylistViewModels.AddRange(vms);
     

      var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(vms, EventAction.Play, this);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
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

    #endregion

    public override void Dispose()
    {
      base.Dispose();
    }
  }
}