using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore;
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
          createThumbnails = new ActionCommand(async () => await OnCreateThumbnails());
        }

        return createThumbnails;
      }
    }

    #region OnCreateThumbnails

    public async Task OnCreateThumbnails()
    {
      if (Thumbnails.Count == 0)
      {
        try
        {
          ThumbnailsLoading = true;
          Thumbnails.Clear();
          var thmbs = new List<ThumbnailViewModel>();

          await Task.Run(async () =>
          {
            var ffprobeResult = await iVFfmpegProvider.RunFfprobe<ProcessOutputHandler>($"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{Model.FullName}\"");

            var allOutput = ffprobeResult.Output.ToList();
            double? fileDuration = null;
            int numberOfScreenshots = 5;

            var thumbsFolder = "preview_thumbnails";

            Directory.CreateDirectory(thumbsFolder);

            var dur = allOutput.FirstOrDefault(x => x != null);

            if (double.TryParse(dur, out var parsed))
            {
              fileDuration = parsed * 0.9;
            }

            if (fileDuration != null)
            {
              int screenInterval = (int)fileDuration / numberOfScreenshots;
              string ffmpegParams = " ";

              for (int i = 0; i < numberOfScreenshots; i++)
              {
                var actualDuration = (i + 1) * screenInterval;

                var time = TimeSpan.FromSeconds(actualDuration);
                var timeStr = TimeSpan.FromSeconds(actualDuration).ToString(@"hh\:mm\:ss");

                ffmpegParams += $"-ss {timeStr} -i \"{Model.FullName}\" ";

                thmbs.Add(new ThumbnailViewModel()
                {
                  Time = time
                });
              }

              for (int i = 0; i < numberOfScreenshots; i++)
              {
                ffmpegParams += $"-map {i}:v -vframes 1 -qscale:v 2 {thumbsFolder}\\output_{i + 1}.png ";
              }

              ffmpegParams = ffmpegParams + " -y";

              var result = await iVFfmpegProvider.RunFfmpeg<ProcessOutputHandler>(ffmpegParams, true);

              for (int i = 0; i < numberOfScreenshots; i++)
              {
                var file = $"{thumbsFolder}\\output_{i + 1}.png";

                if (File.Exists(file))
                {
                  var bytes = File.ReadAllBytes(file);

                  thmbs[i].ImageData = bytes;

                  File.Delete(file);
                }
              }
            }
          });

          Thumbnails.AddRange(thmbs);
        }
        finally

        {
          ThumbnailsLoading = false;
        }
      }
    }

    #endregion

    #endregion

    #endregion

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

      var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)), EventAction.Play, this);

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
  }
}