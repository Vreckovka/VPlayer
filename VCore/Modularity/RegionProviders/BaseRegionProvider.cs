using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using Ninject;
using Prism.Mvvm;
using Prism.Regions;
using PropertyChanged;
using VCore.Annotations;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.ItemsCollections;
using VCore.Modularity.Interfaces;

namespace VCore.Modularity.RegionProviders
{
  public class BaseRegionProvider : IRegionProvider
  {
    #region Fields

    private readonly IRegionManager regionManager;
    private readonly IViewFactory viewFactory;
    protected readonly IViewModelsFactory viewModelsFactory;

    private List<IRegistredView> Views = new List<IRegistredView>();

    #endregion

    #region Constructors

    public BaseRegionProvider(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      [NotNull] IViewModelsFactory viewModelsFactory)
    {
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
      this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #endregion

    #region SubscribeToChanges

    private void SubscribeToChanges<TView, TViewModel>(RegistredView<TView, TViewModel> view)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged
    {
      view.ViewWasActivated.Subscribe((x) => x.Activate());
    }

    #endregion

    #region RegisterView

    public Guid RegisterView<TView, TViewModel>(string regionName, TViewModel viewModel, bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged
    {
      var registredView = Views.SingleOrDefault(x => x.ViewName == RegistredView<TView, TViewModel>.GetViewName());

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

    #endregion

    #region CreateView

    public RegistredView<TView, TViewModel> CreateView<TView, TViewModel>(string regionName, TViewModel viewModel, bool initializeImmediately = false)
      where TViewModel : class, INotifyPropertyChanged
      where TView : class, IView
    {
      return new RegistredView<TView, TViewModel>(regionManager.Regions[regionName], viewFactory, viewModelsFactory, viewModel, initializeImmediately);
    }


    #endregion

    #region ActivateView

    public void ActivateView(Guid guid)
    {
      var view = Views.SingleOrDefault(x => x.Guid == guid);
      view?.Activate();
    }

    #endregion

    #region DectivateView

    public void DectivateView(Guid guid)
    {
      var view = Views.SingleOrDefault(x => x.Guid == guid);
      view?.Deactivate();
    }

    #endregion
  }
}