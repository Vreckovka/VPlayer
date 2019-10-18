using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.Interfaces;
using VCore.Modularity.Navigation;
using VCore.ViewModels;

namespace VCore.Modularity.RegionProviders
{
  public class BaseRegionProvider : IRegionProvider
  {
    #region Fields

    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly INavigationProvider navigationProvider;
    private readonly IRegionManager regionManager;
    private readonly IViewFactory viewFactory;
    private List<IRegistredView> Views = new List<IRegistredView>();
    private Dictionary<IRegistredView,IDisposable> ActivateSubscriptions = new Dictionary<IRegistredView, IDisposable>();

    #endregion Fields

    #region Constructors

    public BaseRegionProvider(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      IViewModelsFactory viewModelsFactory,
      INavigationProvider navigationProvider)
    {
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
      this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.navigationProvider = navigationProvider ?? throw new ArgumentNullException(nameof(navigationProvider));
    }

    #endregion Constructors

    #region Methods

    #region SubscribeToChanges

    private void SubscribeToChanges<TView, TViewModel>(RegistredView<TView, TViewModel> view)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged
    {
      var disposable = view.ViewWasActivated.Subscribe((x) =>
      {
        x.Activate();
        navigationProvider.SetNavigation(x);
      });

      ActivateSubscriptions.Add(view, disposable);
    }

    #endregion SubscribeToChanges

    #region RegisterView

      public Guid RegisterView<TView, TViewModel>(
      string regionName,
      TViewModel viewModel,
      bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged
    {
      var registredView = Views.SingleOrDefault(x => x.ViewName == RegistredView<TView, TViewModel>.GetViewName(regionName));

      if (registredView == null)
      {
        var view = CreateView<TView, TViewModel>(regionName, viewModel, containsNestedRegion);

        SubscribeToChanges(view);

        Views.Add(view);

        return view.Guid;
      }
      else if (registredView is RegistredView<TView, TViewModel> view)
      {
        view.ViewModel = viewModel;
        return view.Guid;
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    #endregion RegisterView

    #region CreateView

    public RegistredView<TView, TViewModel> CreateView<TView, TViewModel>(
      string regionName,
      TViewModel viewModel,
      bool initializeImmediately = false)
      where TViewModel : class, INotifyPropertyChanged
      where TView : class, IView
    {
      var region = regionManager.Regions[regionName];
      return new RegistredView<TView, TViewModel>(region, viewFactory, viewModelsFactory, viewModel, initializeImmediately);
    }

    #endregion CreateView

    #region ActivateView

    public void ActivateView(Guid guid)
    {
      var view = Views.SingleOrDefault(x => x.Guid == guid);
      view?.Activate();
    }

    #endregion ActivateView

    #region DectivateView

    public void DectivateView(
      Guid guid)
    {
      var view = Views.SingleOrDefault(x => x.Guid == guid);
      view?.Deactivate();
    }

    #endregion DectivateView

    #region GoBack

    public void GoBack(Guid guid)
    {
      var view = Views.SingleOrDefault(x => x.Guid == guid);
      var before = navigationProvider.GetPrevious(view);
      before?.Activate();
    }

    #endregion

    #endregion Methods
  }
}