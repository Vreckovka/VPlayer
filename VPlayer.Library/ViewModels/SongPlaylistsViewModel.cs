using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels
{
  public class LoadingStatus : ViewModel
  {
    #region IsLoading

    private bool isLoading;

    public bool IsLoading
    {
      get { return isLoading; }
      set
      {
        if (value != isLoading)
        {
          isLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalProcessCount

    private int totalProcessCount;

    public int TotalProcessCount
    {
      get { return totalProcessCount; }
      set
      {
        if (value != totalProcessCount)
        {
          totalProcessCount = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(Progress));
        }
      }
    }

    #endregion

    #region ProcessedCount

    private int processedCount;

    public int ProcessedCount
    {
      get { return processedCount; }
      set
      {
        if (value != processedCount)
        {
          processedCount = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(Progress));
        }
      }
    }

    #endregion

    #region Progress

    public double Progress
    {
      get { return ProcessedCount * 100.0 / TotalProcessCount; }
     
    }

    #endregion



  }


  public class SongPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, SongsPlaylistViewModel, SoundItemFilePlaylist>
  {
    public SongPlaylistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory, 
      IStorageManager storageManager,
      LibraryCollection<SongsPlaylistViewModel, SoundItemFilePlaylist> libraryCollection,
      IEventAggregator eventAggregator) : 
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      LoadingStatus = new LoadingStatus()
      {
        IsLoading = true,
        ProcessedCount = 5,
        TotalProcessCount = 10
      };
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Music";
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;


    public LoadingStatus LoadingStatus { get;  }
  }
}
