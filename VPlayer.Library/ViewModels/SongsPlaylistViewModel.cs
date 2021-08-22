using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Library.ViewModels;

namespace VPlayer.Home.ViewModels
{
  public class SongsPlaylistViewModel : FilePlaylistViewModel<SoundItemInPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem>
{
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly SongPlaylistsViewModel songPlaylistsViewModel;
    private readonly IStorageManager storageManager;
    private readonly ILogger logger;

    #endregion

    #region Constructors

    public SongsPlaylistViewModel(
      SoundItemFilePlaylist model,
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

    public override IEnumerable<SoundItemInPlaylistViewModel> GetItemsToPlay()
    {
      var playlist = storageManager.GetRepository<SoundItemFilePlaylist>()
        .Include(x => x.PlaylistItems)
        .ThenInclude(x => x.ReferencedItem)
        .SingleOrDefault(x => x.Id == Model.Id);


      if (playlist != null)
      {
        var playlistItems = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList();

        var list = new List<SongInPlayListViewModel>();

        var fristEpisode = storageManager.GetRepository<Song>().Where(x => x.SoundItem.Id == playlistItems[0].IdReferencedItem).Include(x => x.Album).ThenInclude(x => x.Artist).SingleOrDefault();

        if (fristEpisode != null)
        {
          var album = storageManager.GetRepository<Artist>()
            .Where(x => x.Id == fristEpisode.Album.Artist.Id)
            .Include(x => x.Albums)
            .ThenInclude(x => x.Songs)
            .ThenInclude(x => x.SoundItem).Single();

          var songs = album.Albums.SelectMany(x => x.Songs).ToList();

          foreach (var item in playlistItems)
          {
            var song = songs.Single(x => x.SoundItem.Id == item.ReferencedItem.Id);

            list.Add(viewModelsFactory.Create<SongInPlayListViewModel>(song
              ));
          }

          return list;
        }
        else
        {
          return playlistItems.Select(x => viewModelsFactory.Create<SoundItemInPlaylistViewModel>(x.ReferencedItem));
        }
      }

      return null;
    }

    #endregion

    public override void PublishPlayEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels, EventAction eventAction )
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels)
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }


    #region OnPlay

    protected override void OnPlay(EventAction action)
    {
      songPlaylistsViewModel.IsBusy = true;

      Task.Run(() =>
      {
        var data = GetItemsToPlay().ToList();

        var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(data, action, IsShuffle, IsRepeating, Model.LastItemElapsedTime, Model);

        try
        {
          eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
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