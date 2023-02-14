using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using VCore.Standard.Helpers;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Home.Views.Statistics;
using VPlayer.UPnP.Views;

namespace VPlayer.Home.ViewModels.Statistics
{
  public class StatisticsViewModel : RegionViewModel<StatisticsView>
  {
    private readonly IStorageManager storageManager;

    public StatisticsViewModel(IRegionProvider regionProvider, IStorageManager storageManager) : base(regionProvider)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public override string Header => "Statistics";



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
    
    #region View

    private VirtualList<IPlayableModel> view;

    public VirtualList<IPlayableModel> View
    {
      get { return view; }
      set
      {
        if (value != view)
        {
          view = value;
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
      Task.Run(() =>
      {
        var list = new List<IPlayableModel>();
        var sounds = storageManager.GetRepository<SoundItem>().Include(x => x.FileInfo).Where(x => !x.IsPrivate);
        var videos = storageManager.GetRepository<VideoItem>().Where(x => !x.IsPrivate);
        var episodes = storageManager.GetRepository<TvShowEpisode>().Where(x => !x.IsPrivate);

        list.AddRange(sounds);
        list.AddRange(videos);
        list.AddRange(episodes);

        Application.Current.Dispatcher.Invoke(() =>
        {
          TotalWatched = list.Sum(x => x.TimePlayed);
          View = new VirtualList<IPlayableModel>(list.OrderByDescending(x => x.TimePlayed).Take(30), 30);
        });
      });
    }

    #endregion BackCommand

    public override void Initialize()
    {
      base.Initialize();


      OnLoad();
    }
  }
}
