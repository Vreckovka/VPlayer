using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Providers;
using VPlayer.WindowsPlayer.ViewModels.VideoProperties;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using Application = System.Windows.Application;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, VideoItemInPlaylistViewModel, VideoPlaylist, PlaylistVideoItem, VideoItem>
  {
    protected TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();

    public VideoPlayerViewModel(
      IRegionProvider regionProvider,
       IKernel kernel,
       ILogger logger,
      IStorageManager storageManager,
       IEventAggregator eventAggregator,
      IVlcProvider vlcProvider) :
      base(regionProvider, kernel, logger, storageManager, eventAggregator, vlcProvider)
    {
    }

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

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      IsPlaying = false;
      base.Initialize();

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<TvShowEpisodeInPlaylistViewModel>>().Subscribe(RemoveFromPlaystTvShow).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<TvShowEpisodeInPlaylistViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, TvShowEpisodeInPlaylistViewModel>>().Subscribe(PlayTvShowItems).DisposeWith(this);

      aspectRatios = new RxObservableCollection<AspectRatioViewModel>()
      {
        new AspectRatioViewModel("1:1"),
        new AspectRatioViewModel("4:3"),
        new AspectRatioViewModel("16:9"),
        new AspectRatioViewModel("21:9"),
      };

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
        var model = ActualItem.Model;

        model.AspectRatio = selectedItem.Description;

        MediaPlayer.AspectRatio = model.AspectRatio;

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

      });

      if (MediaPlayer.Media != null)
      {
        MediaPlayer.Media.ParsedChanged += MediaPlayer_ParsedChanged;

        if (ActualItem != null)
        {
          MediaPlayer.AspectRatio = ActualItem.Model.AspectRatio;

          if (MediaPlayer.AspectRatio != null)
          {
            var selected = AspectRatios.Single(x => x.Description == MediaPlayer.AspectRatio);
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
            MediaPlayer.SetSpu(ActualItem.Model.AudioTrack.Value);
          }

          var actualAudioTrack = AudioTracks.Single(x => MediaPlayer.AudioTrack == x.Model.Id);

          actualAudioTrack.IsSelected = true;
        }


        if (MediaPlayer.Media != null)
          MediaPlayer.Media.ParsedChanged -= MediaPlayer_ParsedChanged;
      });
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

      var videoNames = PlayList.OfType<VideoItemInPlaylistViewModel>().GroupBy(x => x.Description).Take(2).Select(x => x.Key).ToList();

      nameKeys.AddRange(videoNames);

      var playlistName = string.Join(", ", nameKeys.ToArray());

      if (videoNames.Count > 2)
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
