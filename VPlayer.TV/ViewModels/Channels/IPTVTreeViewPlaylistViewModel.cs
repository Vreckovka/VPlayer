using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.Helpers;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Library.ViewModels;

namespace VPlayer.IPTV.ViewModels
{
  public class IPTVTreeViewPlaylistViewModel : TvTreeViewItem<TvPlaylist>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IWindowManager windowManager;

    public IPTVTreeViewPlaylistViewModel(
      TvPlaylist model,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      IWindowManager windowManager) : base(model, storageManager, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));

      if (Model.PlaylistItems != null)
      {
        foreach (var item in Model.PlaylistItems)
        {
          var tvGroup = storageManager.GetRepository<TvChannelGroup>().Include(x => x.TvChannelGroupItems).ThenInclude(x => x.TvChannel).ThenInclude(x => x.TvItem).SingleOrDefault(x => x.Id == item.IdReferencedItem);

          if (tvGroup != null)
            SubItems.Add(viewModelsFactory.Create<TvChannelGroupPlaylistItemViewModel>(item, tvGroup));
        }

        CanExpand = Model.PlaylistItems.Count > 0;
      }
    }

    #region Commands

    #region TvChannelDropped

    private ActionCommand<object> tvChannelDropped;

    public ICommand TvChannelDropped
    {
      get
      {
        if (tvChannelDropped == null)
        {
          tvChannelDropped = new ActionCommand<object>(OnTvChannelDropped);
        }

        return tvChannelDropped;
      }
    }

    protected override void OnTvChannelDropped(object dropData)
    {
      IDataObject data = dropData as IDataObject;

      if (null == data) return;

      var tvChannelViewModel = data.GetData(data.GetFormats()[0]) as TvChannelGroupViewModel;

      if (tvChannelViewModel == null)
        return;

      if (Model.PlaylistItems == null)
      {
        Model.PlaylistItems = new List<TvPlaylistItem>();
      }

      if (tvChannelViewModel != null && Model.PlaylistItems.All(x => x.Id != tvChannelViewModel.Model.Id))
      {
        var tvPlaylistItem = new TvPlaylistItem()
        {
          IdReferencedItem = tvChannelViewModel.Model.Id,
          Name = tvChannelViewModel.Model.Name
        };


        Model.PlaylistItems.Add(tvPlaylistItem);

        CanExpand = true;
        IsExpanded = true;

        var itemIds = Model.PlaylistItems.Select(x => x.Id);

        var hashCode = itemIds.GetSequenceHashCode();

        Model.HashCode = hashCode;

        RaisePropertyChanged(nameof(SubItems));

        storageManager.UpdatePlaylist<TvPlaylist, TvPlaylistItem>(Model, out var updatedModel);

        tvPlaylistItem.ReferencedItem = tvChannelViewModel.Model.TvItem;
        

        SubItems.Add(viewModelsFactory.Create<TvChannelGroupPlaylistItemViewModel>(tvPlaylistItem, tvChannelViewModel.Model));
      }
    }

    #endregion

    #region Delete

    private ActionCommand delete;

    public ICommand Delete
    {
      get
      {
        if (delete == null)
        {
          delete = new ActionCommand(OnDelete);
        }

        return delete;
      }
    }

    protected virtual void OnDelete()
    {
      var question = windowManager.ShowYesNoPrompt($"Do you really want to delete {Name}?", "Delete tv playlist");

      if (question == System.Windows.MessageBoxResult.Yes)
      {
        var result = storageManager.DeleteEntity(Model);
      }
    }

    #endregion

    #endregion
  }
}