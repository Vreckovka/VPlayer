using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Logger;
using Ninject;
using Prism.Events;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Wpf;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShow;
using VPlayer.WindowsPlayer.Providers;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoPlayerViewModel : PlayableRegionViewModel<VideoPlayerView, TvShowEpisodeInPlaylistViewModel, PlayTvShowEventData, TvShowPlaylist, PlaylistTvShowEpisode, TvShowEpisode>
  {
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

    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;

    public override string Header => "Video Player";

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      EventAggregator.GetEvent<PlayTvShowEvent>().Subscribe(PlayItemsFromEvent).DisposeWith(this);
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
  }
}
