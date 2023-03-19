using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Home.Views.Statistics;
using VPlayer.IPTV.ViewModels;
using VPlayer.UPnP.Views;

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

    private VirtualList<DomainEntity> itemsView;

    public VirtualList<DomainEntity> ItemsView
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

    private VirtualList<DomainEntity> videosItemsView;

    public VirtualList<DomainEntity> VideosItemsView
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

    private VirtualList<DomainEntity> soundsItemsView;

    public VirtualList<DomainEntity> SoundsItemsView
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

    private VirtualList<IPlaylist> playlistView;

    public VirtualList<IPlaylist> PlaylistView
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
      int subCategorySize = 15;
      int allSize = 30;

      return Task.Run(() =>
      {
        var list = new List<IPlayableModel>();
        var sounds = storageManager.GetTempRepository<SoundItem>().Include(x => x.FileInfo).Where(x => !x.IsPrivate).ToList();
        var videos = storageManager.GetTempRepository<VideoItem>().Where(x => !x.IsPrivate).ToList();
        var episodes = storageManager.GetTempRepository<TvShowEpisode>().Where(x => !x.IsPrivate).ToList();

        list.AddRange(sounds);
        list.AddRange(videos);
        list.AddRange(episodes);

        var itemsToDisplay = list.OrderByDescending(x => x.TimePlayed).Take(allSize).OfType<DomainEntity>().ToList();

        var songsItems = storageManager.GetTempRepository<Song>()
          .Where(x => itemsToDisplay.OfType<SoundItem>().Select(y => y.Id).Contains(x.ItemModel.Id))
          .Include(x => x.Album)
          .ThenInclude(x => x.Artist)
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        foreach (var song in songsItems)
        {
          var index = itemsToDisplay.IndexOf(x => x.Id == song.ItemModelId);

          if (index != null)
            itemsToDisplay[index.Value] = song;
        }


        var soundsToDisplay = sounds.OrderByDescending(x => x.TimePlayed).Take(subCategorySize).OfType<DomainEntity>().ToList();

        songsItems = storageManager.GetTempRepository<Song>()
          .Where(x => soundsToDisplay.OfType<SoundItem>().Select(y => y.Id).Contains(x.ItemModel.Id))
          .Include(x => x.Album)
          .ThenInclude(x => x.Artist)
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        foreach (var song in songsItems)
        {
          var index = soundsToDisplay.IndexOf(x => x.Id == song.ItemModelId);

          if (index != null)
            soundsToDisplay[index.Value] = song;
        }


        var videosToDisplay = videos.OrderByDescending(x => x.TimePlayed).Take(subCategorySize).OfType<DomainEntity>().ToList();


        Application.Current.Dispatcher.Invoke(() =>
        {
          TotalWatchedItems = list.Sum(x => x.TimePlayed);
          ItemsView = new VirtualList<DomainEntity>(itemsToDisplay, allSize);
          VideosItemsView = new VirtualList<DomainEntity>(soundsToDisplay, subCategorySize);
          SoundsItemsView = new VirtualList<DomainEntity>(videosToDisplay, subCategorySize);
        });
      });
    }

    private Task LoadPlaylists()
    {
      return Task.Run(() =>
      {
        var list = new List<IPlaylist>();
        var videos = storageManager.GetTempRepository<VideoFilePlaylist>().Where(x => !x.IsPrivate);
        var sounds = storageManager.GetTempRepository<SoundItemFilePlaylist>().Where(x => !x.IsPrivate);
        var episodes = storageManager.GetTempRepository<TvPlaylist>().Where(x => !x.IsPrivate);

        list.AddRange(sounds);
        list.AddRange(videos);
        list.AddRange(episodes);


        Application.Current.Dispatcher.Invoke(() =>
        {
          //Items data were collected later and we don't want to lose old
          //data so we combine Playlist.TotalPlayedTime - Items.Sum(x => x.TimePlayed)
          TotalWatched = list.Sum(x => x.TotalPlayedTime) - TotalWatchedItems;
          PlaylistView = new VirtualList<IPlaylist>(list.OrderByDescending(x => x.TotalPlayedTime).Take(30), 30);
        });
      });
    }
  }
}
