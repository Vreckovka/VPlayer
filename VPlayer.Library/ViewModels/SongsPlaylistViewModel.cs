﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Repositories;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
{
  public class SongsPlaylistViewModel : PlaylistViewModel<SongInPlayListViewModel, SongsPlaylist, PlaylistSong>
{
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly SongPlaylistsViewModel songPlaylistsViewModel;
    private readonly IStorageManager storageManager;
    private readonly ILogger logger;

    #endregion

    #region Constructors

    public SongsPlaylistViewModel(
      SongsPlaylist model,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      SongPlaylistsViewModel songPlaylistsViewModel,
      IStorageManager storageManager,
      ILogger logger) : base(model, eventAggregator, storageManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.songPlaylistsViewModel = songPlaylistsViewModel ?? throw new ArgumentNullException(nameof(songPlaylistsViewModel));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    public override IEnumerable<SongInPlayListViewModel> GetItemsToPlay()
    {
      var playlist = storageManager.GetRepository< SongsPlaylist>().Where(x => x.Id == ModelId)
        .Include(x => x.PlaylistItems).ThenInclude(x => x.Song).ThenInclude(x => x.Album)
        .SingleOrDefault();

      if (playlist != null)
      {
        var validSongs = playlist.PlaylistItems.Where(x => x.Song.Album != null).ToList();

        if (validSongs.Count != playlist.PlaylistItems.Count)
        {
          logger.Log(MessageType.Error, $"SONGS WITH NULL ALBUM! {ModelId} {Name}");
        }

        return validSongs.OrderBy(x => x.OrderInPlaylist).Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x.Song));
      }

      return new List<SongInPlayListViewModel>(); ;
    }

    #endregion

    public override void PublishPlayEvent(IEnumerable<SongInPlayListViewModel> viewModels, EventAction eventAction )
    {
      var e = new PlaySongsEventData(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SongInPlayListViewModel> viewModels)
    {
      var e = new PlaySongsEventData(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }


    #region OnPlay

    protected override void OnPlay(EventAction action)
    {
      songPlaylistsViewModel.IsBusy = true;

      Task.Run(() =>
      {
        var data = GetItemsToPlay().ToList();

        var e = new PlaySongsEventData(data, action, IsShuffle, IsRepeating, Model.LastItemElapsedTime, Model);

        try
        {
          eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
        }
        catch (Exception ex)
        {

          throw;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
          songPlaylistsViewModel.IsBusy = false;
        });
      });
    }

    #endregion

  }
}