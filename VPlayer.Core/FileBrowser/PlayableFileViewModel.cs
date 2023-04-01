using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Helpers;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;
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


    #region PinnedItem

    private PinnedItem pinnedItem;

    public PinnedItem PinnedItem
    {
      get { return pinnedItem; }
      set
      {
        if (value != pinnedItem)
        {
          pinnedItem = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPinned

    private bool isPinned;

    public bool IsPinned
    {
      get { return isPinned; }
      set
      {
        if (value != isPinned)
        {
          isPinned = value;
          RaisePropertyChanged();
          OnIsPinnedChanged(isPinned);
        }
      }
    }

    protected virtual void OnIsPinnedChanged(bool newValue)
    {

    }

    #endregion


    #endregion

    #region Commands

    #region PlayButton

    private ActionCommand<EventAction> playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand<EventAction>(Play);
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

    #region PinItem

    private ActionCommand pinItem;

    public ICommand PinItem
    {
      get
      {
        if (pinItem == null)
        {
          pinItem = new ActionCommand(OnPinItem);
        }

        return pinItem;
      }
    }

    public async void OnPinItem()
    {
      var foundItem = storageManager.GetTempRepository<PinnedItem>().SingleOrDefault(x => x.Description == Model.Indentificator &&
                                                                                          x.PinnedType == GetPinnedType());

      if (foundItem == null)
      {
        var newPinnedItem = new PinnedItem();
        newPinnedItem.Description = Model.Indentificator;
        newPinnedItem.PinnedType = GetPinnedType();

        var item = await Task.Run(() => storageManager.AddPinnedItem(newPinnedItem));

        PinnedItem = item;
      }
    }

    protected PinnedType GetPinnedType()
    {
      return FileType == FileType.Sound ? PinnedType.SoundFile : FileType == FileType.Video ? PinnedType.VideoFile : PinnedType.None;
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

    public void Play(EventAction eventAction)
    {
      if(long.TryParse(Model.Indentificator, out var id))
      {
        Model.Extension = System.IO.Path.GetExtension(Model.FullName.ToLower());
      }
      else
      {
        Model.Extension = System.IO.Path.GetExtension(Model.Indentificator.ToLower());
      }

      FileType = Model.Extension.GetFileType();

      if (FileType == FileType.Video)
      {
        PlayItem<VideoItem, VideoItemInPlaylistViewModel>(eventAction);
      }
      else if (FileType == FileType.Sound)
      {
        PlayItem<SoundItem, SoundItemInPlaylistViewModel>(eventAction);
      }
    }

    #endregion

    #region PlayItems

    private void PlayItem<TModel, TViewModel>(EventAction eventAction)
      where TModel : PlayableItem, IUpdateable<TModel>, new()
      where TViewModel : FileItemInPlayList<TModel>
    {
      TModel entityItem = storageManager.GetTempRepository<TModel>()
        .Include(x => x.FileInfoEntity)
        .SingleOrDefault(x => x.FileInfoEntity.Source == Model.Indentificator);

      if (entityItem == null)
      {
        var pVideoItem = new TModel()
        {
          Name = Model.Name,
        };

        FileInfoEntity fileInfoEntity = new FileInfoEntity()
        {
          Name = Model.Name,
          Indentificator = Model.Indentificator,
          Source = Model.Source
        };

        pVideoItem.FileInfoEntity = fileInfoEntity;
        storageManager.StoreEntity(pVideoItem, out entityItem);
      }

      if (entityItem == null)
        return;

      if (entityItem.FileInfoEntity == null)
      {
        FileInfoEntity fileInfoEntity = new FileInfoEntity()
        {
          Name = Model.Name,
          Indentificator = Model.Indentificator,
        };

        entityItem.FileInfoEntity = fileInfoEntity;
      }


      var vms = viewModelsFactory.Create<TViewModel>(entityItem);

      vms.ObservePropertyChange(x => x.IsInPlaylist).ObserveOnDispatcher().Subscribe((x) =>
      {
        IsInPlaylist = x;
      }).DisposeWith(this);

      var data = new PlayItemsEventData<TViewModel>(vms.AsList(), eventAction, this);

      eventAggregator.GetEvent<PlayItemsEvent<TModel, TViewModel>>().Publish(data);
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