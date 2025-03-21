﻿using LibVLCSharp.Shared;
using Logger;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ninject;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MovieCollection.OpenSubtitles;
using MovieCollection.OpenSubtitles.Models;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Providers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VFfmpeg;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Players;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.ViewModels.VideoProperties;
using VPlayer.WindowsPlayer.ViewModels.Windows;
using VPlayer.WindowsPlayer.Views.Prompts;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using VVLC.Players;
using Application = System.Windows.Application;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using System.Net;
using HtmlAgilityPack;
using System.Globalization;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoPlayerViewModel : FilePlayableRegionViewModel<WindowsPlayerView, VideoItemInPlaylistViewModel, VideoFilePlaylist, PlaylistVideoItem, VideoItem, VideoSliderPopupDetailViewModel>
  {
    private readonly IWindowManager windowManager;
    private readonly ICSFDWebsiteScrapper iCsfdWebsiteScrapper;
    private TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();

    #region Constructors

    public VideoPlayerViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      VLCPlayer vLCPlayer,
      IWindowManager windowManager,
      IStatusManager statusManager,
      ICSFDWebsiteScrapper iCsfdWebsiteScrapper,
      IViewModelsFactory viewModelsFactory,
      IVFfmpegProvider iVFfmpegProvider,
      ISettingsProvider settingsProvider,
      IVPlayerCloudService cloudService) :
      base(regionProvider, kernel, logger, storageManager, eventAggregator, windowManager, statusManager, viewModelsFactory,
        iVFfmpegProvider, settingsProvider, cloudService, vLCPlayer)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.iCsfdWebsiteScrapper = iCsfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(iCsfdWebsiteScrapper));
    }

    #endregion

    #region Properties

    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override bool ContainsNestedRegions => true;
    public override string Header => "Video player";

    #region MediaPlayer

    public new MediaPlayer MediaPlayer
    {
      get { return ((VLCPlayer)base.MediaPlayer).MediaPlayer; }
    }

    #endregion

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
        Title = "Play from stream",
        StreamUrl = System.Windows.Clipboard.GetText()
      };

      windowManager.ShowPrompt<PlayFromStreamView>(vm);

      if (vm.PromptResult == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
      {
        var fileInfo = new FileInfoEntity()
        {
          Source = vm.StreamUrl,
          Indentificator = vm.StreamUrl,

        };

        var item = new VideoItemInPlaylistViewModel(new VideoItem()
        {
          Name = "Stream file",
          FileInfoEntity = fileInfo,
          Duration = 0,
        }, eventAggregator, storageManager);

        item.IsStream = true;

        try
        {
          PlayList.Clear();
          ActualSavedPlaylist = new VideoFilePlaylist();

          PlayList.Add(item);

          RequestReloadVirtulizedPlaylist();

          RaisePropertyChanged(nameof(CanPlay));

          SetItemAndPlay(PlayList.IndexOf(item), true);

        }
        catch (Exception ex)
        {
          windowManager.ShowErrorPrompt(ex);

          PlayList.Remove(item);

          ActualItem = null;

          RequestReloadVirtulizedPlaylist();
        }
      }
    }

    #endregion

    #region AddSubtitles

    private ActionCommand addSubtitles;

    public ICommand AddSubtitles
    {
      get
      {
        if (addSubtitles == null)
        {
          addSubtitles = new ActionCommand(OnAddSubtitles);
        }

        return addSubtitles;
      }
    }

    public void OnAddSubtitles()
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = false;
      dialog.Title = "Add subtitiles";
      dialog.InitialDirectory = GetDefaultFolder();

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        MediaPlayer.ESAdded += MediaPlayer_ESAdded;

        var path = new Uri(dialog.FileName);

        MediaPlayer.AddSlave(MediaSlaveType.Subtitle, path.AbsoluteUri, true);


      }
    }

    #endregion

    #region DownloadSubtitles

    private ActionCommand downloadSubtitles;

    public ICommand DownloadSubtitles
    {
      get
      {
        if (downloadSubtitles == null)
        {
          downloadSubtitles = new ActionCommand(OnDownloadSubtitles);
        }

        return downloadSubtitles;
      }
    }

    public void OnDownloadSubtitles()
    {
      var model = viewModelsFactory.Create<FindSubtitlesPromptViewModel>(ActualItem);

      windowManager.ShowPrompt<FindSubtitlesView>(model);

      if (model?.Download?.Link != null)
      {
        MediaPlayer.ESAdded += MediaPlayer_ESAdded;

        var path = model.Download.Link;

        MediaPlayer.AddSlave(MediaSlaveType.Subtitle, path.AbsoluteUri, true);
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      IsPlaying = false;
      base.InitializeAsync();

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

    private async void OnSubtitleSelected(SubtitleViewModel selectedItem)
    {
      if (selectedItem == null)
      {
        MediaPlayer.SetSpu(-1);
        lastSPUValue = -1;
        return;
      }

      if (MediaPlayer.Spu != selectedItem.Model.Id)
      {
        MediaPlayer.SetSpu(selectedItem.Model.Id);

        if (ActualItem != null)
        {
          var model = ActualItem.Model;

          model.SubtitleTrack = selectedItem.Model.Id;

          await storageManager.UpdateEntityAsync(model);
        }
      }

      lastSPUValue = selectedItem.Model.Id;
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
        audioTrack = model.AudioTrack.Value;

        Task.Run(() => storageManager.UpdateEntityAsync(model));
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

    public override async void OnActivation(bool firstActivation)
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

    public override void OnNewItemPlay(VideoItem videoItem)
    {
      base.OnNewItemPlay(videoItem);

      VSynchronizationContext.PostOnUIThread(() =>
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

      if (DetailViewModel != null && ActualItem != null)
      {
        DetailViewModel.DisablePopup = ActualItem.IsStream;
      }


    }

    #endregion

    #region Media_DurationChanged

    protected override void Media_DurationChanged(object sender, MediaDurationChangedArgs e)
    {
      base.Media_DurationChanged(sender, e);

      if (DetailViewModel != null && ActualItem != null)
      {
        DetailViewModel.DisablePopup = ActualItem.IsStream;
      }
    }

    #endregion

    #region DownloadItemInfo

    protected override async Task DownloadItemInfo(CancellationToken cancellationToken)
    {
      if (ActualItem?.IsStream != true)
      {
        await base.DownloadItemInfo(cancellationToken);

        try
        {
          await FindOnCsfd(ActualItem, cancellationToken);
        }
        catch (Exception)
        {
        }

        await SetPlalistCover();
      }
    }

    #endregion

    #region SetPlalistCover

    private async Task SetPlalistCover()
    {
      if (ActualItem?.ExtraData is CSFDItemViewModel cSFDItem)
      {
        ActualSavedPlaylist.CoverPath = cSFDItem.Model.ImagePath;

        await UpdateActualSavedPlaylistPlaylist();
      }
    }

    #endregion

    #region FindOnCsfd

    private async Task FindOnCsfd(VideoItemInPlaylistViewModel viewModel, CancellationToken cancellationToken)
    {
      if (viewModel == null || viewModel.CSFDItem != null)
      {
        return;
      }

      CSFDItem item = null;

      if (viewModel is TvShowEpisodeInPlaylistViewModel episodeInPlaylistViewModel)
      {
        item = await iCsfdWebsiteScrapper.GetBestFind(
          episodeInPlaylistViewModel.Name,
          cancellationToken,
          tvShowUrl: episodeInPlaylistViewModel?.TvShow.CsfdUrl,
          seasonNumber: episodeInPlaylistViewModel?.TvShowSeason.SeasonNumber,
          episodeNumber: episodeInPlaylistViewModel?.TvShowEpisode.EpisodeNumber);
      }
      else
      {
        bool singleSeason = false;
        var acutalEpisode = DataLoader.GetTvShowSeriesNumber(viewModel.Name);

        if (acutalEpisode?.EpisodeNumber != null)
        {
          var episodeInSeason = PlayList.Select(x => DataLoader.GetTvShowSeriesNumber(x.Name))
            .Where(x => x != null)
            .Count(x => x.SeasonNumber == acutalEpisode.SeasonNumber);

          singleSeason = episodeInSeason > 1 && episodeInSeason <= 20;
        }


        item = await iCsfdWebsiteScrapper.GetBestFind(viewModel.Name, cancellationToken, downloadSingleSeason: singleSeason);
      }

      VSynchronizationContext.PostOnUIThread(() =>
      {
        if (item != null)
        {
          if (item is CSFDTVShow cSFDTVShow)
          {
            if (cSFDTVShow.Seasons == null)
            {
              return;
            }

            if (cSFDTVShow.Seasons.Count > 0)
            {
              var isSingleSeason = cSFDTVShow.Seasons.Where(x => x.SeasonEpisodes != null).Count(x => x.SeasonEpisodes.Count > 0) == 1;

              if (isSingleSeason)
              {
                var singleSeason = cSFDTVShow.Seasons.Where(x => x.SeasonEpisodes != null).Single(x => x.SeasonEpisodes.Count > 0);

                var episodeS = singleSeason?.SeasonEpisodes.First();

                if (singleSeason?.SeasonEpisodes?.Count == 1 && episodeS != null)
                {
                  UpdateVideoItem(viewModel, episodeS, item.Name);
                }
                else if (singleSeason?.SeasonEpisodes != null)
                {
                  var episodeInSeason = PlayList.Where(x => DataLoader.GetTvShowSeriesNumber(x.Name)?.SeasonNumber == singleSeason.SeasonNumber);

                  foreach (var episode in episodeInSeason)
                  {
                    var number = DataLoader.GetTvShowSeriesNumber(episode.Name);

                    if (number != null)
                    {
                      var csfdEpisode = singleSeason.SeasonEpisodes.SingleOrDefault(x => x.EpisodeNumber == number.EpisodeNumber);

                      UpdateVideoItem(episode, csfdEpisode, item.Name);
                    }
                  }
                }
              }
              else
              {
                foreach (var tvShowItem in PlayList.Where(x => DataLoader.IsTvShow(x.Name)))
                {
                  var number = DataLoader.GetTvShowSeriesNumber(tvShowItem.Name);

                  CSFDTVShowSeasonEpisode csfdEpisode = null;

                  if (number != null)
                  {
                    if (number.SeasonNumber >= cSFDTVShow.Seasons.Count && cSFDTVShow.Seasons[number.SeasonNumber.Value - 1].SeasonEpisodes.Count >= number.EpisodeNumber)
                    {
                      csfdEpisode = cSFDTVShow.Seasons[number.SeasonNumber.Value - 1].SeasonEpisodes[number.EpisodeNumber.Value - 1];
                    }

                    UpdateVideoItem(tvShowItem, csfdEpisode, item.Name);
                  }
                }
              }
            }
          }
          else
          {
            UpdateVideoItem(viewModel, item, item.Name);
          }
        }
        else
        {
          MarkViewModelAsChecked(viewModel);
        }
      });
    }

    #endregion

    #region UpdateVideoItem

    private void UpdateVideoItem(VideoItemInPlaylistViewModel tvShowItem, CSFDItem csfdEpisode, string tvShowName)
    {
      if (csfdEpisode != null)
      {
        tvShowItem.CSFDItem = new CSFDItemViewModel(csfdEpisode);

        if (tvShowItem.CSFDItem.Model.OriginalName == tvShowName)
        {
          tvShowItem.CSFDItem.Model.OriginalName = tvShowItem.CSFDItem.Model.Name;
        }
        else
        {
          tvShowItem.CSFDItem.Model.OriginalName = string.IsNullOrEmpty(csfdEpisode.OriginalName) ? csfdEpisode.Name : csfdEpisode.OriginalName;
        }

        MarkViewModelAsChecked(tvShowItem);

        if (!string.IsNullOrEmpty(tvShowItem.CSFDItem.Model.ImagePath))
        {
          ActualSavedPlaylist.CoverPath = tvShowItem.CSFDItem.Model.ImagePath;
          UpdateActualSavedPlaylistPlaylist();
        }
      }
    }

    #endregion

    #region MediaPlayer_ParsedChanged

    private SemaphoreSlim parseChangedSemaphore = new SemaphoreSlim(1, 1);
    private int? audioTrack;
    private async void MediaPlayer_ParsedChanged(object sender, EventArgs e)
    {

      try
      {
        await parseChangedSemaphore.WaitAsync();

        await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
        {
          AudioTracks.Clear();
        });

        if (MediaPlayer.AudioTrackDescription.Length > 0)
        {
          await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
          {
            foreach (var spu in MediaPlayer.AudioTrackDescription)
            {
              var vm = new AudioTrackViewModel(spu);

              vm.Language = GetLanguange(vm);

              AudioTracks.Add(vm);
            }
          });

          if (ActualItem?.Model.AudioTrack != null)
          {
            MediaPlayer.SetAudioTrack(ActualItem.Model.AudioTrack.Value);
            audioTrack = ActualItem.Model.AudioTrack.Value;
          }
          else
          {
            if (audioTrack != null && audioTracks.Count > audioTrack.Value)
            {
              MediaPlayer.SetAudioTrack(audioTrack.Value);
            }
            else
            {
              var audioSetting = TryGetSettingOrGetPreffered(AudioTracks, Language.Czech);

              MediaPlayer.SetAudioTrack(audioSetting.Model.Id);
            }
          }

          var actualAudioTrack = AudioTracks.SingleOrDefault(x => MediaPlayer.AudioTrack == x.Model.Id);

          if (actualAudioTrack != null)
            actualAudioTrack.IsSelected = true;
        }

        await VSynchronizationContext.InvokeOnDispatcherAsync(async () => { await ReloadSubtitles(); });

        if (MediaPlayer.Media != null)
          MediaPlayer.Media.ParsedChanged -= MediaPlayer_ParsedChanged;

        //SelectAspectCropRatios();
      }
      finally
      {
        parseChangedSemaphore.Release();
      }

    }

    #endregion

    #region MediaPlayer_ESAdded

    private void MediaPlayer_ESAdded(object sender, MediaPlayerESAddedEventArgs e)
    {
      if (e.Type == TrackType.Text)
      {
        MediaPlayer.ESAdded -= MediaPlayer_ESAdded;

        ActualItem.Model.SubtitleTrack = MediaPlayer.SpuDescription.Max(x => x.Id);

        Task.Run(async () =>
        {
          Thread.Sleep(500);

          await ReloadSubtitles();
        });

      }
    }

    #endregion

    protected override void OnPlayPlaylist(PlayItemsEventData<VideoItemInPlaylistViewModel> data)
    {
      FillerData.Clear();
      base.OnPlayPlaylist(data);

      lastSPUValue = PlayList.Take(actualItemIndex + 1).LastOrDefault(x => x.Model.SubtitleTrack != null)?.Model.SubtitleTrack;

      //One piece
      if (ActualSavedPlaylist.Id == 1301)
      {
        GetFillerList();
      }

    }

    #region ReloadSubtitles

    private int? lastSPUValue = null;
    private async Task ReloadSubtitles()
    {
      await VSynchronizationContext.InvokeOnDispatcherAsync(async () =>
      {
        Subtitles.Clear();
        SubtitleViewModel subtitleSetting = null;

        if (MediaPlayer.SpuDescription.Length > 0)
        {
          foreach (var spu in MediaPlayer.SpuDescription)
          {
            Subtitles.Add(new SubtitleViewModel(spu));
          }

          if (ActualItem?.Model.SubtitleTrack != null &&
           Subtitles.SingleOrDefault(x => x.Model.Id == ActualItem?.Model.SubtitleTrack) != null)
          {
            MediaPlayer.SetSpu(ActualItem.Model.SubtitleTrack.Value);
            lastSPUValue = ActualItem.Model.SubtitleTrack.Value;
          }
          if (Subtitles.Count >= 2)
          {
            if (lastSPUValue != null)
            {
              subtitleSetting = Subtitles.FirstOrDefault(x => x.Model.Id == lastSPUValue);
            }

            if (subtitleSetting == null)
            {
              var selectedAudio = AudioTracks.FirstOrDefault(x => x.IsSelected);

              if (selectedAudio != null)
              {
                subtitleSetting = TryGetSettingOrGetPreffered(Subtitles, Language.Enghlish);

                if (subtitleSetting.Language == Language.Czech && selectedAudio.Language == Language.Czech)
                {
                  subtitleSetting = Subtitles.FirstOrDefault(x => x.Model.Id == -1);
                }
              }
            }

            if (subtitleSetting != null)
            {
              OnSubtitleSelected(subtitleSetting);
            }
          }

          SubtitleViewModel actualSub = null;

          if (lastSPUValue == null)
          {
            actualSub = Subtitles.SingleOrDefault(x => MediaPlayer.Spu == x.Model.Id);
          }
          else
          {
            actualSub = Subtitles.SingleOrDefault(x => lastSPUValue.Value == x.Model.Id);
          }

          if (actualSub != null)
          {
            actualSub.IsSelected = true;
            lastSPUValue = actualSub.Model.Id;
          }
        }
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
      if (a != 0 && b != 0)
      {
        var gcd = GCD(a, b);

        return string.Format("{0}:{1}", a / gcd, b / gcd);
      }

      return "";
    }

    public int GCD(int a, int b)
    {
      return b == 0 ? Math.Abs(a) : GCD(b, a % b);
    }

    #endregion

    #region TryGetSettingOrGetPreffered

    private TSettingViewModel TryGetSettingOrGetPreffered<TSettingViewModel>(IEnumerable<TSettingViewModel> allItems, Language settingLanguage)
      where TSettingViewModel : class, IViewModel, IDescriptedEntity, IOrdered
    {
      var list = allItems.ToList();
      var foundSettings = new List<TSettingViewModel>();

      var czechSetting = GetLanguageViewModel(list, Language.Czech);
      var englishSetting = GetLanguageViewModel(list, Language.Enghlish);

      foundSettings.Add(czechSetting);
      foundSettings.Add(englishSetting);

      if (settingLanguage == Language.Czech && czechSetting != null)
      {
        return czechSetting;
      }
      else if (englishSetting != null)
      {
        return englishSetting;
      }

      return foundSettings.Concat(list).Where(x => x != null).OrderByDescending(x => x.OrderNumber).FirstOrDefault();
    }

    #endregion

    #region GetLanguageViewModel

    private TSettingViewModel GetLanguageViewModel<TSettingViewModel>(IEnumerable<TSettingViewModel> allItems, Language settingLanguage)
      where TSettingViewModel : class, IViewModel, IDescriptedEntity
    {
      var list = allItems.ToList();
      IEnumerable<TSettingViewModel> items = null;

      switch (settingLanguage)
      {
        case Language.Czech:
          {
            items = list.Where(x =>
            {
              var desc = x.Description.RemoveDiacritics().ToLower();

              return desc.Contains("czech") || desc.Contains("cesky");
            });
            break;
          }
        case Language.Enghlish:
          {
            items = list.Where(x =>
            {
              var desc = x.Description.RemoveDiacritics().ToLower();

              return (desc.Contains("anglicky") || desc.Contains("english"));
            });
            break;
          }
        default:
          throw new ArgumentOutOfRangeException(nameof(settingLanguage), settingLanguage, null);
      }

      if (items.Count() > 1)
      {
        return items.FirstOrDefault(x =>
        !x.Description.RemoveDiacritics().ToLower().Contains("sing") &&
        !x.Description.RemoveDiacritics().ToLower().Contains("song")
        );
      }
      else
        return items.FirstOrDefault();
    }

    #endregion

    #region GetLanguange

    private Language GetLanguange<TSettingViewModel>(TSettingViewModel viewModel)
      where TSettingViewModel : class, IViewModel, IDescriptedEntity
    {
      var desc = viewModel.Description.RemoveDiacritics().ToLower();

      if (desc.Contains("czech") || desc.Contains("cesky"))
      {
        return Language.Czech;
      }
      else if (desc.Contains("anglicky") || desc.Contains("english"))
      {
        return Language.Enghlish;
      }

      return Language.Other;

    }

    #endregion

    public async override void PlayNext()
    {
      base.PlayNext();

      await Task.Delay(200);
      SetOnePieceEpisode();
    }


    private void SetOnePieceEpisode()
    {
      var data = ActualItem.FillerData;

      if (data != null)
      {
        var split = data.StartTime.Split(":");
        var miliseconds = new TimeSpan(0, int.Parse(split[0]), int.Parse(split[1]));

        if (MediaPlayer.Length == 0)
        {
          MediaPlayer.Media.ParsedChanged += async (sender, args) => 
          {
          
            MediaPlayer.Position = (float)miliseconds.TotalMilliseconds / MediaPlayer.Length;
          };
        }
        else
        {
          var startPosition = miliseconds.TotalMilliseconds / MediaPlayer.Length;
          MediaPlayer.Position = (float)startPosition;
        }
      }
    }

    protected override IEnumerable<VideoItemInPlaylistViewModel> GetValidItemsForCloud(IEnumerable<VideoItemInPlaylistViewModel> validItems)
    {
      return validItems.ToList();
    }


    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel> args)
    {
    }

    protected override void ItemsRemoved(EventPattern<VideoItemInPlaylistViewModel> eventPattern)
    {
    }

    protected override void OnActualItemChanged()
    {
      base.OnActualItemChanged();

      if (ActualItem?.ExtraData is CSFDItem cSFDItem)
      {
        ActualSavedPlaylist.CoverPath = cSFDItem.ImagePath;
      }
    }

    #region FilterByActualSearch

    private bool ifFiltered = false;
    protected override void FilterByActualSearch(string predictate)
    {
      if (!string.IsNullOrEmpty(predictate))
      {
        ifFiltered = true;
        var items = PlayList.Where(x =>
          IsInFind(x.Name, predictate) || (x is TvShowEpisodeInPlaylistViewModel tvShowEpisodeInPlaylistViewModel && (
          IsInFind(tvShowEpisodeInPlaylistViewModel.TvShow.Name, predictate) ||
          IsInFind(tvShowEpisodeInPlaylistViewModel.TvShowSeason.Name, predictate) ||
          ("season " + tvShowEpisodeInPlaylistViewModel.TvShowSeason.SeasonNumber == predictate) ||
          "episode " + tvShowEpisodeInPlaylistViewModel.TvShowEpisode.EpisodeNumber == predictate) ||
          IsInFind(x.Model.Source, predictate)));

        var generator = new ItemsGenerator<VideoItemInPlaylistViewModel>(items, 15);

        VirtualizedPlayList = new VirtualList<VideoItemInPlaylistViewModel>(generator);

      }
      else if (ifFiltered)
      {
        RequestReloadVirtulizedPlaylist();
        ifFiltered = false;
      }
    }

    #endregion

    #region GetNewPlaylistModel

    protected override VideoFilePlaylist GetNewPlaylistModel(List<PlaylistVideoItem> playlistModels, bool isUserCreated)
    {
      List<string> nameKeys = new List<string>();

      var artists = PlayList.OfType<TvShowEpisodeInPlaylistViewModel>().GroupBy(x => x.TvShow.Name);

      nameKeys = artists.Select(x => x.Key).ToList();

      if (nameKeys.Count == 0)
      {
        var videoNames = PlayList.OfType<VideoItemInPlaylistViewModel>().GroupBy(x => x.Description).Take(2).Select(x => x.Key).ToList();

        nameKeys.AddRange(videoNames);
      }

      string playlistName = "Created: " + DateTime.Now.ToString("dd.MM.yyyy hh:mm");

      if (nameKeys.Any())
      {
        playlistName = nameKeys.First();

        if (playlistModels.Count == 1)
        {
          playlistName = PlayList.OfType<VideoItemInPlaylistViewModel>().First().Name;
        }
      }


      var entityPlayList = new VideoFilePlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        ItemCount = playlistModels?.Count,
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
        playlistVideoItem.IdReferencedItem = itemViewModel.Model.Id;

      }
      else
      {
        return null;
      }


      return playlistVideoItem;
    }

    #endregion

    protected override Task BeforePlayEvent(PlayItemsEventData<VideoItemInPlaylistViewModel> data)
    {
      audioTrack = null;

      return base.BeforePlayEvent(data);
    }

    protected override void SortPlaylist(PlaylistSortOrder playlistSort)
    {
      base.SortPlaylist(playlistSort);

      switch (playlistSort)
      {
        case PlaylistSortOrder.ItemProperties:
          var comp = Comparer<VideoItemInPlaylistViewModel>.Create((x, y) =>
          {
            int? seasonNumber1 = null;
            int? episodeNumber1 = null;

            int? seasonNumber2 = null;
            int? episodeNumber2 = null;

            if (x == null && y != null)
              return 1;
            if (y == null && x != null)
              return 0;

            var name = x.Name.CompareTo(y.Name);

            if (x is TvShowEpisodeInPlaylistViewModel episode1)
            {
              seasonNumber1 = episode1.TvShowSeason.SeasonNumber;
              episodeNumber1 = episode1.TvShowEpisode.EpisodeNumber;
            }

            if (y is TvShowEpisodeInPlaylistViewModel episode2)
            {
              seasonNumber2 = episode2.TvShowSeason.SeasonNumber;
              episodeNumber2 = episode2.TvShowEpisode.EpisodeNumber;
            }

            if (seasonNumber1 == null)
            {
              var tvShowEpisodeNumbers = DataLoader.GetTvShowSeriesNumber(x.Name);

              seasonNumber1 = tvShowEpisodeNumbers?.SeasonNumber;
              episodeNumber1 = tvShowEpisodeNumbers?.EpisodeNumber;
            }

            if (seasonNumber2 == null)
            {
              var tvShowEpisodeNumbers = DataLoader.GetTvShowSeriesNumber(y.Name);

              seasonNumber2 = tvShowEpisodeNumbers?.SeasonNumber;
              episodeNumber2 = tvShowEpisodeNumbers?.EpisodeNumber;
            }


            var season = seasonNumber1?.CompareTo(seasonNumber2) ?? 0;
            var episode = episodeNumber1?.CompareTo(episodeNumber2) ?? 0;

            return season != 0 ? season : episode != 0 ? episode : name;
          });

          PlayList.Sort(comp);
          break;
      }
    }



    private List<OnePieceEpisodeData> FillerData = new List<OnePieceEpisodeData>();
    private void GetFillerList()
    {
      var html = new WebClient().DownloadString("https://www.animefillerguide.com/2019/06/one-piece-filler-list.html");

      var document = new HtmlDocument();
      document.LoadHtml(html);
      var xpath = "/html/body/div/div[2]/div[1]/main/div[1]/div/div[1]/div/div[2]/div/article/div[2]/table/tbody/tr";

      var episodes = document.DocumentNode.SelectNodes(xpath);

      FillerData = episodes.Where(x => x.ChildNodes.Count > 1).Select(x =>
      {
        try
        {
          var chapters = x.ChildNodes[3].InnerText.Trim();

          return new OnePieceEpisodeData()
          {
            EpisodeNumber = int.Parse(x.ChildNodes[0].InnerText.TrimStart('0').Replace(".", null)),
            StartTime = x.ChildNodes[1].InnerText.Replace("(", null).Replace(")", null),
            Name = x.ChildNodes[2].InnerText.Trim(),
            Chapters = chapters,
            IsFiller = chapters == "N/A"
          };
        }
        catch (Exception ex)
        {
          return null;
        }
      }).Where(x => x != null).ToList();

      foreach (var item in PlayList)
      {
        var tvShowEpisodeNumbers = DataLoader.GetTvShowSeriesNumber(item.Name);

        if (tvShowEpisodeNumbers?.EpisodeNumber != null)
        {
          var data = FillerData.FirstOrDefault(x => x.EpisodeNumber == tvShowEpisodeNumbers.EpisodeNumber);
          item.FillerData = data;
        }
      }
    }
    #endregion
  }
}
