using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Providers;
using VPlayer.WindowsPlayer.ViewModels.VideoProperties;
using VPlayer.WindowsPlayer.ViewModels.Windows;
using VPlayer.WindowsPlayer.Views.Prompts;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using Application = System.Windows.Application;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, VideoItemInPlaylistViewModel, VideoPlaylist, PlaylistVideoItem, VideoItem>
  {
    private readonly IWindowManager windowManager;
    protected TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();

    #region Constructors

    public VideoPlayerViewModel(
      IRegionProvider regionProvider,
       IKernel kernel,
       ILogger logger,
      IStorageManager storageManager,
       IEventAggregator eventAggregator,
      IVlcProvider vlcProvider,
      IWindowManager windowManager) :
      base(regionProvider, kernel, logger, storageManager, eventAggregator, vlcProvider)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    #endregion

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override bool ContainsNestedRegions => true;
    public override string Header => "Video Player";

    #region PlayerViewModel

    private object playerViewModel;

    public object PlayerViewModel
    {
      get { return playerViewModel; }
      set
      {
        if (value != playerViewModel)
        {
          playerViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Subtitles

    private RxObservableCollection<SubtitleViewModel> subtitles = new RxObservableCollection<SubtitleViewModel>();

    public RxObservableCollection<SubtitleViewModel> Subtitles
    {
      get { return subtitles; }
      set
      {
        if (value != subtitles)
        {
          subtitles = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AudioTracks

    private RxObservableCollection<AudioTrackViewModel> audioTracks = new RxObservableCollection<AudioTrackViewModel>();

    public RxObservableCollection<AudioTrackViewModel> AudioTracks
    {
      get { return audioTracks; }
      set
      {
        if (value != audioTracks)
        {
          audioTracks = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AspectRatios

    private RxObservableCollection<AspectRatioViewModel> aspectRatios;

    public RxObservableCollection<AspectRatioViewModel> AspectRatios
    {
      get { return aspectRatios; }
      set
      {
        if (value != aspectRatios)
        {
          aspectRatios = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CropRatios

    private RxObservableCollection<AspectRatioViewModel> cropRatios;

    public RxObservableCollection<AspectRatioViewModel> CropRatios
    {
      get { return cropRatios; }
      set
      {
        if (value != cropRatios)
        {
          cropRatios = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsBuffering

    private bool isBuffering;

    public bool IsBuffering
    {
      get { return isBuffering; }
      set
      {
        if (value != isBuffering)
        {
          isBuffering = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region VideoViewInitlized

    private ActionCommand videoViewInitlized;

    public ICommand VideoViewInitlized
    {
      get
      {
        if (videoViewInitlized == null)
        {
          videoViewInitlized = new ActionCommand(OnVideoViewInitlized);
        }

        return videoViewInitlized;
      }
    }

    public void OnVideoViewInitlized()
    {
      if (!loadedTask.Task.IsCompleted)
        loadedTask.SetResult(true);
    }

    #endregion

    #region PlayFromStream

    private ActionCommand playFromStream;

    public ICommand PlayFromStream
    {
      get
      {
        if (playFromStream == null)
        {
          playFromStream = new ActionCommand(OnPlayFromStream);
        }

        return playFromStream;
      }
    }

    public void OnPlayFromStream()
    {
      var vm = new PlayFromStreamViewModel()
      {
        Title = "Play from stream"
      };

      windowManager.ShowPrompt<PlayFromStreamView>(vm);

      if (vm.PromptResult == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        var item = new VideoItemInPlaylistViewModel(new VideoItem()
        {
          Name = "Stream file",
          DiskLocation = vm.StreamUrl,
          Duration = (int)new TimeSpan(99, 99, 99).TotalSeconds
        }, eventAggregator, storageManager);

        try
        {
          PlayList.Clear();
          ActualSavedPlaylist = new VideoPlaylist();

          PlayList.Add(item);

          ReloadVirtulizedPlaylist();

          RaisePropertyChanged(nameof(CanPlay));

          SetItemAndPlay(PlayList.IndexOf(item), true);
        }
        catch (Exception ex)
        {
          windowManager.ShowPrompt(ex.ToString(), "Error");

          PlayList.Remove(item);

          ActualItem = null;

          ReloadVirtulizedPlaylist();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override async void Initialize()
    {
      IsPlaying = false;
      await base.InitializeAsync();

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<TvShowEpisodeInPlaylistViewModel>>().Subscribe(RemoveFromPlaystTvShow).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<TvShowEpisodeInPlaylistViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, TvShowEpisodeInPlaylistViewModel>>().Subscribe(PlayTvShowItems).DisposeWith(this);


      var ratios = new AspectRatioViewModel[]
      {
        new AspectRatioViewModel("Default")
          {
            IsDefault = true
          },
        new AspectRatioViewModel("1:1"),
        new AspectRatioViewModel("4:3"),
        new AspectRatioViewModel("16:9"),
        new AspectRatioViewModel("21:9"),
        new AspectRatioViewModel("2.35:1")
          {
            Value = "235:100"
          },
        new AspectRatioViewModel("2.39:1")
        {
          Value = "239:100"
        },
      };

      aspectRatios = new RxObservableCollection<AspectRatioViewModel>();
      cropRatios = new RxObservableCollection<AspectRatioViewModel>();

      foreach (var ratio in ratios)
      {
        aspectRatios.Add(ratio.Copy());
        cropRatios.Add(ratio.Copy());
      }


      Subtitles.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(VideoProperty.IsSelected))
        .Select(x => (SubtitleViewModel)x.Sender)
         .Where(x => x.IsSelected)
        .Subscribe(x =>
        {
          MakeSingleSelection(Subtitles, x);
          OnSubtitleSelected(x);
        }).DisposeWith(this);

      AudioTracks.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(VideoProperty.IsSelected))
        .Select(x => (AudioTrackViewModel)x.Sender)
        .Where(x => x.IsSelected)
        .Subscribe(x =>
        {
          MakeSingleSelection(AudioTracks, x);
          OnAudioTrackSelected(x);
        }).DisposeWith(this);

      AspectRatios.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(VideoProperty.IsSelected))
        .Select(x => (AspectRatioViewModel)x.Sender)
        .Where(x => x.IsSelected)
        .Subscribe(x =>
        {
          MakeSingleSelection(AspectRatios, x);
          OnAspectRatioSelected(x);
        }).DisposeWith(this);

      CropRatios.ItemUpdated
        .Where(x => x.EventArgs.PropertyName == nameof(VideoProperty.IsSelected))
        .Select(x => (AspectRatioViewModel)x.Sender)
        .Where(x => x.IsSelected)
        .Subscribe(x =>
        {
          MakeSingleSelection(CropRatios, x);
          OnCropRatioSelected(x);
        }).DisposeWith(this);

      MediaPlayer.Buffering += MediaPlayer_Buffering;
    }


    private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
    {
      if (e.Cache != 100)
        IsBuffering = true;
      else
        IsBuffering = false;
    }


    #endregion

    #region PlayTvShowItems

    private void PlayTvShowItems(PlayItemsEventData<TvShowEpisodeInPlaylistViewModel> tvShowEpisodeInPlaylistViewModel)
    {
      var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(tvShowEpisodeInPlaylistViewModel.Items, tvShowEpisodeInPlaylistViewModel.EventAction, tvShowEpisodeInPlaylistViewModel.Model);

      PlayItemsFromEvent(data);
    }

    #endregion

    #region RemoveFromPlaystTvShow

    private void RemoveFromPlaystTvShow(RemoveFromPlaylistEventArgs<TvShowEpisodeInPlaylistViewModel> obj)
    {
      var data = new RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel>()
      {
        DeleteType = obj.DeleteType,
        ItemsToRemove = obj.ItemsToRemove.Select(x => (VideoItemInPlaylistViewModel)x).ToList()
      };

      RemoveItemsFromPlaylist(data);
    }

    #endregion

    #region MakeSingleSelection

    private void MakeSingleSelection<TItem>(RxObservableCollection<TItem> items, TItem selectedItem) where TItem : class, INotifyPropertyChanged, ISelectable
    {
      var notSelected = items.Where(x => x != selectedItem).ToList();

      notSelected.ForEach(x => x.IsSelected = false);
    }

    #endregion

    #region OnSubtitleSelected

    private void OnSubtitleSelected(SubtitleViewModel selectedItem)
    {
      MediaPlayer.SetSpu(selectedItem.Model.Id);

      if (ActualItem != null)
      {
        var model = ActualItem.Model;

        model.SubtitleTrack = selectedItem.Model.Id;

        storageManager.UpdateEntityAsync(model);
      }
    }

    #endregion

    #region OnAudioTrackSelected

    private void OnAudioTrackSelected(AudioTrackViewModel selectedItem)
    {
      MediaPlayer.SetAudioTrack(selectedItem.Model.Id);

      if (ActualItem != null)
      {
        var model = ActualItem.Model;

        model.AudioTrack = selectedItem.Model.Id;

        storageManager.UpdateEntityAsync(model);
      }
    }

    #endregion

    #region OnAspectRatioSelected

    private void OnAspectRatioSelected(AspectRatioViewModel selectedItem)
    {
      if (ActualItem != null)
      {
        var ratio = selectedItem.Value;
        var model = ActualItem.Model;

        if (selectedItem.IsDefault)
        {
          ratio = null;
        }

        model.AspectRatio = ratio;

        MediaPlayer.AspectRatio = model.AspectRatio;

        storageManager.UpdateEntityAsync(model);
      }
    }

    #endregion

    #region OnCropRatioSelected

    private void OnCropRatioSelected(AspectRatioViewModel selectedItem)
    {
      if (ActualItem != null)
      {
        var ratio = selectedItem.Value;
        var model = ActualItem.Model;

        if (selectedItem.IsDefault)
        {
          ratio = null;
        }

        model.CropRatio = selectedItem.Description;

        MediaPlayer.CropGeometry = ratio;
        
        storageManager.UpdateEntityAsync(model);
      }
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        var view = regionProvider.RegisterView<VideoPlayerView, VideoPlayerViewModel>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }
    }

    #endregion

    #region WaitForInitilization

    protected override Task WaitForVlcInitilization()
    {
      return loadedTask.Task;
    }

    #endregion

    #region OnNewItemPlay

    public override void OnNewItemPlay()
    {
      base.OnNewItemPlay();

      Application.Current.Dispatcher.Invoke(() =>
      {
        AspectRatios.ForEach(x => x.IsSelected = false);
        CropRatios.ForEach(x => x.IsSelected = false);
      });

      if (MediaPlayer.Media != null)
      {
        MediaPlayer.Media.ParsedChanged += MediaPlayer_ParsedChanged;

        if (ActualItem != null)
        {
          MediaPlayer.AspectRatio = ActualItem.Model.AspectRatio;
          MediaPlayer.CropGeometry = ActualItem.Model.CropRatio;

          if (MediaPlayer.AspectRatio != null)
          {
            var selected = AspectRatios.Single(x => x.Description == MediaPlayer.AspectRatio);
            selected.IsSelected = true;
          }

          if (MediaPlayer.CropGeometry != null)
          {
            var selected = CropRatios.Single(x => x.Description == MediaPlayer.CropGeometry);
            selected.IsSelected = true;
          }
        }
      }
    }

    #endregion

    #region MediaPlayer_ParsedChanged

    private void MediaPlayer_ParsedChanged(object sender, EventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {

        Subtitles.Clear();
        AudioTracks.Clear();

        if (MediaPlayer.SpuDescription.Length > 0)
        {
          foreach (var spu in MediaPlayer.SpuDescription)
          {
            Subtitles.Add(new SubtitleViewModel(spu));
          }

          if (ActualItem?.Model.SubtitleTrack != null)
          {
            MediaPlayer.SetSpu(ActualItem.Model.SubtitleTrack.Value);
          }

          var actualSub = Subtitles.Single(x => MediaPlayer.Spu == x.Model.Id);

          actualSub.IsSelected = true;

        }


        if (MediaPlayer.AudioTrackDescription.Length > 0)
        {
          foreach (var spu in MediaPlayer.AudioTrackDescription)
          {
            AudioTracks.Add(new AudioTrackViewModel(spu));
          }

          if (ActualItem?.Model.AudioTrack != null)
          {
            MediaPlayer.SetAudioTrack(ActualItem.Model.AudioTrack.Value);
          }

          var actualAudioTrack = AudioTracks.Single(x => MediaPlayer.AudioTrack == x.Model.Id);

          actualAudioTrack.IsSelected = true;
        }


        if (MediaPlayer.Media != null)
          MediaPlayer.Media.ParsedChanged -= MediaPlayer_ParsedChanged;

        SelectAspectCropRatios();
      });
    }

    #endregion

    #region SelectAspectCropRatios

    private void SelectAspectCropRatios()
    {
      uint width = 0;
      uint height = 0;
      MediaPlayer.Size(0, ref width, ref height);

      var aspectRation = GetRatio((int)width, (int)height);


      if (AspectRatios.SingleOrDefault(x => x.IsSelected) == null)
      {
        var actualRatio = AspectRatios.SingleOrDefault(x => x.Description == aspectRation);

        if (actualRatio != null)
        {
          actualRatio.IsSelected = true;
        }

        if (actualRatio == null)
        {
          actualRatio = AspectRatios.Single(x => x.IsDefault);

          actualRatio.IsSelected = true;
        }
      }

      if (CropRatios.SingleOrDefault(x => x.IsSelected) == null)
      {
        var actualRatio = CropRatios.SingleOrDefault(x => x.Description == aspectRation);

        if (actualRatio != null)
        {
          actualRatio.IsSelected = true;
        }

        if (actualRatio == null)
        {
          actualRatio = CropRatios.Single(x => x.IsDefault);

          actualRatio.IsSelected = true;
        }
      }
    }

    #endregion

    #region GetRatio

    public string GetRatio(int a, int b)
    {
      var gcd = GCD(a, b);

      return string.Format("{0}:{1}", a / gcd, b / gcd);
    }

    public int GCD(int a, int b)
    {
      return b == 0 ? Math.Abs(a) : GCD(b, a % b);
    }

    #endregion


    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel> args)
    {
    }

    protected override void ItemsRemoved(EventPattern<VideoItemInPlaylistViewModel> eventPattern)
    {
    }

    protected override void FilterByActualSearch(string predictate)
    {
      throw new NotImplementedException();
    }

    #region GetNewPlaylistModel

    protected override VideoPlaylist GetNewPlaylistModel(List<PlaylistVideoItem> playlistModels, bool isUserCreated)
    {
      List<string> nameKeys = new List<string>();

      var artists = PlayList.OfType<TvShowEpisodeInPlaylistViewModel>().GroupBy(x => x.TvShow.Name);

      nameKeys = artists.Select(x => x.Key).ToList();

      if (nameKeys.Count == 0)
      {
        var videoNames = PlayList.OfType<VideoItemInPlaylistViewModel>().GroupBy(x => x.Description).Take(2).Select(x => x.Key).ToList();

        nameKeys.AddRange(videoNames);
      }

      var playlistName = string.Join(", ", nameKeys.ToArray());

      if (nameKeys.Count > 2)
      {
        playlistName += " ...";
      }

      var entityPlayList = new VideoPlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        ItemCount = playlistModels.Count,
        PlaylistItems = playlistModels,
        LastItemElapsedTime = ActualSavedPlaylist.LastItemElapsedTime,
        LastItemIndex = ActualSavedPlaylist.LastItemIndex,
        IsUserCreated = isUserCreated,
        LastPlayed = DateTime.Now
      };

      return entityPlayList;


      return null;
    }

    #endregion

    #region GetNewPlaylistItemViewModel

    protected override PlaylistVideoItem GetNewPlaylistItemViewModel(VideoItemInPlaylistViewModel itemViewModel, int index)
    {
      var playlistVideoItem = new PlaylistVideoItem();

      if (itemViewModel.Model.Id != 0)
      {
        playlistVideoItem.IdVideoItem = itemViewModel.Model.Id;
      }
      else
      {
        return null;
      }


      playlistVideoItem.OrderInPlaylist = (index + 1);


      return playlistVideoItem;
    }

    #endregion 

    #endregion
  }
}
