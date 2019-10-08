using Prism.Regions;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.Interfaces;
using VCore.ViewModels;

namespace VCore.Modularity.RegionProviders
{
  public class RegistredView<TView, TViewModel> : IDisposable, IRegistredView
    where TView : class, IView
    where TViewModel : class, INotifyPropertyChanged
  {

  #region Fields

  private readonly bool initializeImmediately;
  private readonly IViewFactory viewFactory;
  private readonly IViewModelsFactory viewModelsFactory;
  private readonly SerialDisposable viewWasActivatedDisposable;
  private readonly SerialDisposable viewWasDeactivatedDisposable;

  #endregion Fields

  #region Constructors

  public RegistredView(
    IRegion region,
    IViewFactory viewFactory,
    IViewModelsFactory viewModelsFactory,
    TViewModel viewModel = null
  ) : this(region, viewFactory, viewModelsFactory, viewModel, false)
  {
  }

  public RegistredView(
    IRegion region,
    IViewFactory viewFactory,
    IViewModelsFactory viewModelsFactory,
    TViewModel viewModel = null,
    bool initializeImmediately = false
  )
  {
    this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
    this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    this.initializeImmediately = initializeImmediately;

    viewWasActivatedDisposable = new SerialDisposable();
    viewWasDeactivatedDisposable = new SerialDisposable();

    Guid = Guid.NewGuid();

    ViewModel = viewModel;
    ViewName = GetViewName(region.Name);
    Region = region;

    if (initializeImmediately)
    {
      View = RegisterView();
    }
  }

  #endregion Constructors

  #region Properties

  public Guid Guid { get; }
  public IRegion Region { get; set; }
  public TView View { get; set; }
  public string ViewName { get; set; }
  public Subject<IRegistredView> ViewWasActivated { get; } = new Subject<IRegistredView>();
  public Subject<IRegistredView> ViewWasDeactivated { get; } = new Subject<IRegistredView>();

  #region ViewModel

  private TViewModel viewModel;

  public TViewModel ViewModel
  {
    get { return viewModel; }
    set
    {
      if (value != viewModel)
      {
        viewModel = value;

        viewWasActivatedDisposable.Disposable = Observable
          .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            x => ViewModel.PropertyChanged += x,
            x => ViewModel.PropertyChanged -= x)
          .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) &&
                      ((IActivable) x.Sender).IsActive)
          .Subscribe((
            x) => ViewWasActivated.OnNext(this));

        viewWasDeactivatedDisposable.Disposable = Observable
          .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            x => ViewModel.PropertyChanged += x,
            x => ViewModel.PropertyChanged -= x)
          .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) &&
                      !((IActivable) x.Sender).IsActive)
          .Subscribe((
            x) => ViewWasDeactivated.OnNext(this));

        if (View != null)
          View.DataContext = viewModel;
      }
    }
  }

  #endregion ViewModel

  #endregion Properties

  #region Methods

  #region Activate

  public void Activate()
  {
    Region.Activate(RegisterView());

    if (ViewModel is IActivable activable && !activable.IsActive)
    {
      activable.IsActive = true;
    }
  }

  #endregion Activate

  #region Deactivate

  public void Deactivate()
  {
    Region.Deactivate(View);
  }

  #endregion Deactivate

  #region RegisterView

  public TView RegisterView()
  {
    if (View == null)
    {
      View = Create();

      if (ViewModel == null)
        ViewModel = viewModelsFactory.Create<TViewModel>();

      Region.Add(View, ViewName);
    }

    View.DataContext = null;
    View.DataContext = ViewModel;

    return View;
  }

  #endregion RegisterView

  #region Create

  public TView Create()
  {
    return viewFactory.Create<TView>();
  }

  #endregion Create

  #region GetViewName

  public static string GetViewName(string regionName)
  {
    return typeof(TView).Name + "_" + typeof(TViewModel).Name + "_" + regionName;
  }

  #endregion GetViewName

  #region ToString

  public override string ToString()
  {
    return ViewName;
  }

  #endregion ToString

  #region Dispose

  public void Dispose()
  {
    viewWasActivatedDisposable?.Dispose();
    viewWasDeactivatedDisposable?.Dispose();
    ViewWasActivated?.Dispose();
    ViewWasDeactivated?.Dispose();
  }

  #endregion Dispose

  #endregion Methods

  }
}