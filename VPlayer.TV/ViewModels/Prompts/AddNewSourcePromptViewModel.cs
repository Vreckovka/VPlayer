using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels.Prompt;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels.Prompts
{
  public class AddNewSourcePromptViewModel : PromptViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private RxObservableCollection<TVSourceViewModel> sourceViewModels = new RxObservableCollection<TVSourceViewModel>();

    #region Constructors

    public AddNewSourcePromptViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      sourceViewModels.Add(viewModelsFactory.Create<SourceTvSourceViewModel>(new TvSource()
      {
        TvSourceType = TVSourceType.Source,
        TvChannels = new List<TvChannel>()
      }));

      sourceViewModels.Add(viewModelsFactory.Create <M3UTvSourceViewModel>(new TvSource()
      {
        TvSourceType = TVSourceType.M3U
      }));

      sourceViewModels.Add(viewModelsFactory.Create <IptvStalkerTvSourceViewModel>(new TvSource()
      {
        TvSourceType = TVSourceType.IPTVStalker
      }));

      SelectedTvSourceType = TVSourceType.Source;

      SelectedTvSourceViewModel = sourceViewModels.SingleOrDefault(x => x.TvSourceType == SelectedTvSourceType);

      sourceViewModels.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(TVSourceViewModel.IsValid)).Select(x => x.Sender).Subscribe(x =>
      {
        okCommand.RaiseCanExecuteChanged();
      });

      CanExecuteOkCommand = () =>
      {
        return SelectedTvSourceViewModel != null && SelectedTvSourceViewModel.IsValid;
      };

    }

    #endregion


    #region SourceType

    private TVSourceType selectedTvSourceType;

    public TVSourceType SelectedTvSourceType
    {
      get { return selectedTvSourceType; }
      set
      {
        if (value != selectedTvSourceType)
        {
          selectedTvSourceType = value;
          SelectedTvSourceViewModel = sourceViewModels.SingleOrDefault(x => x.TvSourceType == SelectedTvSourceType);
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SelectedSourceViewModel

    private TVSourceViewModel selectedTvSourceViewModel;

    public TVSourceViewModel SelectedTvSourceViewModel
    {
      get { return selectedTvSourceViewModel; }
      set
      {
        if (value != selectedTvSourceViewModel)
        {
          selectedTvSourceViewModel = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}
