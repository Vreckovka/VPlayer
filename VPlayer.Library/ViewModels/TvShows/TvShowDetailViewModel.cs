using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Home.Views.TvShows;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class TvShowDetailViewModel : DetailViewModel<TvShowViewModel, TvShow, TvShowDetailView>
  {
    private readonly IWindowManager windowManager;
    private readonly IViewModelsFactory viewModelsFactory;

    public TvShowDetailViewModel(
      IRegionProvider regionProvider,
       IStorageManager storageManager,
      TvShowViewModel model,
     IWindowManager windowManager,
      IStatusManager statusManager,
      IViewModelsFactory viewModelsFactory) : base(regionProvider, storageManager, statusManager, model, windowManager)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public List<TvShowSeason> Seasons
    {
      get
      {
        return ViewModel?.Model?.Seasons;
      }
    }

    #region AddToPlaylist

    private ActionCommand addNewSeason;

    public ICommand AddNewSeason
    {
      get
      {
        if (addNewSeason == null)
        {
          addNewSeason = new ActionCommand(OnAddNewSeason);
        }

        return addNewSeason;
      }
    }

    public void OnAddNewSeason()
    {
      var vm = viewModelsFactory.Create<AddNewTvShowSeasonViewModel>(ViewModel.Model);

      windowManager.ShowPrompt<TvShowDetailAddNewSeason>(vm);
    }

    #endregion 

    #region OnUpdate

    protected override void OnUpdate()
    {
      var vm = viewModelsFactory.Create<UpdateTvShowViewModel>(ViewModel.Model);

      windowManager.ShowPrompt<TvShowUpdateView>(vm);

    }

    #endregion


    protected override void OnDbUpdate(IItemChanged<TvShow> itemChanged)
    {
      base.OnDbUpdate(itemChanged);

      RaisePropertyChanged(nameof(Seasons));
    }

    protected override Task LoadEntity()
    {
      return Task.CompletedTask;
    }
  }
}
