using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels
{
  public class SoundItemPlaylistsViewModel : PlaylistsViewModel<PlaylistsView, SongsPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem, SoundItem>
  {
    private readonly ISettingsProvider settingsProvider;
    private bool wasSet = false;
    public SoundItemPlaylistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      ISettingsProvider settingsProvider,
      LibraryCollection<SongsPlaylistViewModel, SoundItemFilePlaylist> libraryCollection,
      IEventAggregator eventAggregator) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Music";
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;

    protected override IQueryable<PlaylistSoundItem> GetActualItemQuery => base.GetActualItemQuery.Include(x => x.ReferencedItem.FileInfo);

    protected override void OnDataLoaded()
    {
      base.OnDataLoaded();
      if (!int.TryParse(settingsProvider.Settings[GlobalSettings.MaxItemsForDefaultPlaylist]?.Value, out var max))
      {
        max = 500;
      }

#if !DEBUG
      if (!wasSet)
      {
        var playlist = LibraryCollection.Items
          .Where(x => x.ItemsCount < max)
          .OrderByDescending(x => x.LastPlayed)
          .FirstOrDefault();

        playlist?.OnPlayButton(Core.Events.EventAction.InitSetPlaylist);
        wasSet = true;
      }
#endif
    }

    protected override List<PinnedItem> GetPinnedTypedItems(List<PinnedItem> pinnedItems)
    {
      return pinnedItems.Where(x => 
        x.PinnedType == PinnedType.SoundPlaylist ||
        x.PinnedType == PinnedType.SoundFolder ||
        x.PinnedType == PinnedType.SoundFile ||
        x.PinnedType == PinnedType.Artist ||
        x.PinnedType == PinnedType.Album
        ).ToList();
    }
  }
}
