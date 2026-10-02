using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.Views.Statistics;

namespace VPlayer.Home.ViewModels.Statistics
{
  public class StatisticsViewModel : RegionViewModel<StatisticsView>
  {
    private readonly IStorageManager storageManager;

    public StatisticsViewModel(IRegionProvider regionProvider, IStorageManager storageManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      LoadingStatus = new LoadingStatus()
      {
        ShowProcessCount = false
      };
    }

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public override string Header => "Statistics";


    public LoadingStatus LoadingStatus { get; protected set; }

    #region TotalWatched

    private TimeSpan totalWatched;

    public TimeSpan TotalWatched
    {
      get { return totalWatched; }
      set
      {
        if (value != totalWatched)
        {
          totalWatched = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalWatchedItems

    private TimeSpan totalWatchedItems;

    public TimeSpan TotalWatchedItems
    {
      get { return totalWatchedItems; }
      set
      {
        if (value != totalWatchedItems)
        {
          totalWatchedItems = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ItemsView

    private IReadOnlyList<DomainEntity> itemsView;

    public IReadOnlyList<DomainEntity> ItemsView
    {
      get { return itemsView; }
      set
      {
        if (value != itemsView)
        {
          itemsView = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    #region VideosItemsView

    private IReadOnlyList<DomainEntity> videosItemsView;

    public IReadOnlyList<DomainEntity> VideosItemsView
    {
      get { return videosItemsView; }
      set
      {
        if (value != videosItemsView)
        {
          videosItemsView = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SoundsItemsView

    private IReadOnlyList<DomainEntity> soundsItemsView;

    public IReadOnlyList<DomainEntity> SoundsItemsView
    {
      get { return soundsItemsView; }
      set
      {
        if (value != soundsItemsView)
        {
          soundsItemsView = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    #region PlaylistView

    private IReadOnlyList<IPlaylist> playlistView;

    public IReadOnlyList<IPlaylist> PlaylistView
    {
      get { return playlistView; }
      set
      {
        if (value != playlistView)
        {
          playlistView = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    #region BackCommand

    private ActionCommand load;

    public ICommand Load
    {
      get
      {
        if (load == null)
        {
          load = new ActionCommand(OnLoad);
        }

        return load;
      }
    }

    protected virtual void OnLoad()
    {
      LoadData();
    }

    #endregion BackCommand
    
    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        await LoadData();
      }
    }

    private async Task LoadData()
    {
      try
      {
        LoadingStatus.IsLoading = true;
        await LoadItems();
        await LoadPlaylists();
      }
      finally 
      {
        LoadingStatus.IsLoading = false;
      }
    }

    private Task LoadItems()
    {
      return Task.Run(() =>
      {
        var snapshot=StatisticsQueries.LoadItems(storageManager);
        VSynchronizationContext.PostOnUIThread(() =>
        {
          TotalWatchedItems=snapshot.Total;
          ItemsView=snapshot.Items;
          SoundsItemsView=snapshot.Sounds;
          VideosItemsView=snapshot.Videos;
        });
      });
    }

    private Task LoadPlaylists()
    {
      return Task.Run(() =>
      {
        var snapshot=StatisticsQueries.LoadPlaylists(storageManager);
        VSynchronizationContext.PostOnUIThread(() =>
        {
          // Preserve the legacy playlist history adjustment.
          TotalWatched=snapshot.Total-TotalWatchedItems;
          PlaylistView=snapshot.Items;
        });
      });
    }
  }
}