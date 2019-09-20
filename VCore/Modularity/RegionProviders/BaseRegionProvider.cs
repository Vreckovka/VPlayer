using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
using VCore.ViewModels;

namespace VCore.Modularity.RegionProviders
{
  public class BaseRegionProvider : IRegionProvider
  {
    private readonly IRegionManager regionManager;
    private readonly IViewFactory viewFactory;
    private readonly IViewModelsFactory viewModelsFactory;

    private List<IRegistredView> Views = new List<IRegistredView>();

    public BaseRegionProvider(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      [NotNull] IViewModelsFactory viewModelsFactory)
    {
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
      this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    private void SubscribeToChanges<TView, TViewModel>(RegistredView<TView, TViewModel> view)
      where TView : class, IView
      where TViewModel : class, IRegionViewModel<TView>
    {
      view.ViewWasActivated.Subscribe((x) => x.Activate());
      view.ViewWasDeactivated.Subscribe((x) => x.Deactivate());
    }

    public void RegisterView<TView, TViewModel>(string name, TViewModel viewModel, bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, IRegionViewModel<TView>
    {
      var view = CreateView<TView, TViewModel>(name, viewModel, containsNestedRegion);

      SubscribeToChanges(view);

      Views.Add(view);
    }


    public RegistredView<TView, TViewModel> CreateView<TView, TViewModel>(string regionName, TViewModel viewModel, bool initializeImmediately = false)
      where TViewModel : class, IRegionViewModel<TView>
      where TView : class, IView
    {
      return new RegistredView<TView, TViewModel>(regionManager.Regions[regionName], viewFactory, viewModelsFactory, viewModel, initializeImmediately);
    }
  }
}


public class RegistredView<TView, TViewModel> : IRegistredView
  where TView : class, IView
  where TViewModel : class, IRegionViewModel<TView>
{
  private readonly IViewFactory viewFactory;
  private readonly IViewModelsFactory viewModelsFactory;

  public RegistredView(
    IRegion region,
    [NotNull] IViewFactory viewFactory,
    [NotNull] IViewModelsFactory viewModelsFactory,
    TViewModel viewModel = null,
    bool initializeImmediately = false
    )
  {
    this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
    this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

    ViewModel = viewModel;
    ViewName = typeof(TView).Name;
    Region = region;

    if (initializeImmediately)
    {
      View = RegisterView();
    }

    Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
        x => ViewModel.PropertyChanged += x,
        x => ViewModel.PropertyChanged -= x)
      .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) && ((IActivable)x.Sender).IsActive)
      .Subscribe((x) => ViewWasActivated.OnNext(this));

    Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
        x => ViewModel.PropertyChanged += x,
        x => ViewModel.PropertyChanged -= x)
      .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) && !((IActivable)x.Sender).IsActive)
      .Subscribe((x) => ViewWasDeactivated.OnNext(this));

  }

  public string ViewName { get; set; }
  public IRegion Region { get; set; }
  public Subject<IRegistredView> ViewWasActivated { get; } = new Subject<IRegistredView>();
  public Subject<IRegistredView> ViewWasDeactivated { get; } = new Subject<IRegistredView>();
  public TView View { get; set; }
  public TViewModel ViewModel { get; set; }

  public void Activate()
  {
    Region.Activate(RegisterView());
  }

  public void Deactivate()
  {
    Region.Deactivate(View);
  }

  public TView RegisterView()
  {
    if (View == null)
    {
      View = Create();

      if (ViewModel == null)
        ViewModel = viewModelsFactory.Create<TViewModel>();

      View.DataContext = ViewModel;

      Region.Add(View, ViewName);
    }

    return View;
  }

  public TView Create()
  {
    return viewFactory.Create<TView>();
  }

  public event PropertyChangedEventHandler PropertyChanged;

  [NotifyPropertyChangedInvocator]
  protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}

public interface IRegistredView
{
  Subject<IRegistredView> ViewWasActivated { get; }
  Subject<IRegistredView> ViewWasDeactivated { get; }

  void Activate();

  void Deactivate();
}

