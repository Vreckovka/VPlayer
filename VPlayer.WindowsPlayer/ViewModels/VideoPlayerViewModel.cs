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
using VCore.Annotations;
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
  public class VideoPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, TvShowEpisodeInPlaylistViewModel, PlayTvShowEventData, TvShowPlaylist, PlaylistTvShowEpisode, TvShowEpisode>
  {
    protected TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();

    public VideoPlayerViewModel(
      IRegionProvider regionProvider,
      [NotNull] IKernel kernel,
      [NotNull] ILogger logger,
      [NotNull] IStorageManager storageManager,
      [NotNull] IEventAggregator eventAggregator,
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

      EventAggregator.GetEvent<PlayTvShowEvent>().Subscribe(PlayItemsFromEvent).DisposeWith(this);

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

        Subtitles.Clear();
        AudioTracks.Clear();
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

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<TvShowEpisodeInPlaylistViewModel> args)
    {
    }

    protected override void ItemsRemoved(EventPattern<TvShowEpisodeInPlaylistViewModel> eventPattern)
    {
    }

    protected override void FilterByActualSearch(string predictate)
    {
      throw new NotImplementedException();
    }

    #region GetNewPlaylistModel

    protected override TvShowPlaylist GetNewPlaylistModel(List<PlaylistTvShowEpisode> playlistModels, bool isUserCreated)
    {
      var artists = PlayList.GroupBy(x => x.TvShow.Name);

      var playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());

      var entityPlayList = new TvShowPlaylist()
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
    }

    #endregion

    #region GetNewPlaylistItemViewModel

    protected override PlaylistTvShowEpisode GetNewPlaylistItemViewModel(TvShowEpisodeInPlaylistViewModel itemViewModel, int index)
    {
      return new PlaylistTvShowEpisode()
      {
        IdTvShowEpisode = itemViewModel.Model.Id,
        OrderInPlaylist = (index + 1)
      };
    }

    #endregion 

    #endregion
  }
}
