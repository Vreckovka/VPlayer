using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.WPF.Interfaces.Managers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPlayer.IPTV;
using VPlayer.IPTV.ViewModels;
using VPlayer.Library.ViewModels;

namespace VPlayer.Home.ViewModels.IPTV
{
  public class IPTVPlaylistViewModel : PlaylistViewModel<TvItemInPlaylistItemViewModel, TvPlaylist, TvPlaylistItem>
  {
    private readonly IVPlayerViewModelsFactory viewModelsFactory;

    public IPTVPlaylistViewModel(
      TvPlaylist model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      IVPlayerViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(model, eventAggregator, storageManager, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    public override Task<IEnumerable<TvItemInPlaylistItemViewModel>> GetItemsToPlay()
    {
      return Task.Run(() =>
      {
        var groups = storageManager.GetRepository<TvChannelGroup>().Include(x => x.TvChannelGroupItems).ThenInclude(x => x.TvChannel).ThenInclude(x => x.TvSource).Include(x => x.TvItem);

        var items = storageManager.GetRepository<TvPlaylist>()
          .Include(x => x.PlaylistItems)
          .ThenInclude(x => x.ReferencedItem)
          .SelectMany(x => x.PlaylistItems);

        var list = new List<TvItemInPlaylistItemViewModel>();

        foreach (var item in items)
        {
          var group = groups.SingleOrDefault(x => x.Id == item.ReferencedItem.Id);

          if (group != null)
          {
            var groupViewModel = viewModelsFactory.Create<TvChannelGroupViewModel>(group);
            groupViewModel.Initialize();

            foreach (var groupItemViewModel in groupViewModel.SubItems.ViewModels)
            {
              groupItemViewModel.Initialize();
            }

            groupViewModel.SelectedTvChannel.IsSelected = true;

            list.Add(viewModelsFactory.CreateTvItemInPlaylistItemViewModel(groupViewModel.Model.TvItem, groupViewModel));
          }
        }

        return list.AsEnumerable();
      });

    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<TvItemInPlaylistItemViewModel> viewModels, EventAction eventAction)
    {
      var eventToPublis = eventAggregator.GetEvent<PlayItemsEvent<TvItem, TvItemInPlaylistItemViewModel>>();

      var data = new PlayItemsEventData<TvItemInPlaylistItemViewModel>(viewModels, EventAction.Play, this)
      {
        StorePlaylist = false,
        SetItemOnly = true
      };


      eventToPublis.Publish(data);
    }

    #endregion

    public override void PublishAddToPlaylistEvent(IEnumerable<TvItemInPlaylistItemViewModel> viewModels)
    {
      throw new NotImplementedException();
    }

    public void Update(TvPlaylistItem updateItem)
    {
      throw new NotImplementedException();
    }
  }
}