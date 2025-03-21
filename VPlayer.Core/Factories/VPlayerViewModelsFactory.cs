﻿using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using Ninject.Parameters;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.Core.Factories
{
  public class VPlayerViewModelsFactory : BaseViewModelsFactory, IVPlayerViewModelsFactory
  {
    public VPlayerViewModelsFactory(IKernel kernel) : base(kernel)
    {
    }

    public TvShowEpisodeInPlaylistViewModel CreateTvShowEpisodeInPlayList(VideoItem videoItem, TvShowEpisode tvShowEpisode)
    {
      var pVideoItem = new ConstructorArgument("model", videoItem);
      var ptvShowEpisode = new ConstructorArgument("tvShowEpisode", tvShowEpisode);

      return kernel.Get<TvShowEpisodeInPlaylistViewModel>(pVideoItem, ptvShowEpisode);
    }

    public SongInPlayListViewModel CreateSongInPlayListViewModel(SoundItem soundItem, Song song)
    {
      var pVideoItem = new ConstructorArgument("soundItem", soundItem);
      var ptvShowEpisode = new ConstructorArgument("model", song);

      return kernel.Get<SongInPlayListViewModel>(pVideoItem, ptvShowEpisode);
    }

    public TvItemInPlaylistItemViewModel CreateTvItemInPlaylistItemViewModel(TvItem model, ITvPlayableItem tvPlayableItem)
    {
      var pVideoItem = new ConstructorArgument(nameof(model), model);
      var ptvShowEpisode = new ConstructorArgument(nameof(tvPlayableItem), tvPlayableItem);

      return kernel.Get<TvItemInPlaylistItemViewModel>(pVideoItem, ptvShowEpisode);
    }
  }
}
