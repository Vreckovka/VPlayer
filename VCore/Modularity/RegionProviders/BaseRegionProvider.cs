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

        #region SubscribeToChanges

        private void SubscribeToChanges<TView, TViewModel>(RegistredView<TView, TViewModel> view)
          where TView : class, IView
          where TViewModel : class, INotifyPropertyChanged
        {
            view.ViewWasActivated.Subscribe((x) => x.Activate());
            view.ViewWasDeactivated.Subscribe((x) => x.Deactivate());
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
            else if(registredView is RegistredView<TView, TViewModel> view)
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

        public void ActivateView(Guid guid)
        {
            var view = Views.SingleOrDefault(x => x.Guid == guid);
            view?.Activate();
        }

        public void DectivateView(Guid guid)
        {
            var view = Views.SingleOrDefault(x => x.Guid == guid);
            view?.Deactivate();
        }
    }
}

public class RegistredView<TView, TViewModel> : IRegistredView
  where TView : class, IView
  where TViewModel : class, INotifyPropertyChanged
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

        Guid = Guid.NewGuid();

        ViewModel = viewModel;
        ViewName = GetViewName();
        Region = region;

        if (initializeImmediately)
        {
            View = RegisterView();
        }

      

    }

    public string ViewName { get; set; }
    public IRegion Region { get; set; }
    public Subject<IRegistredView> ViewWasActivated { get; } = new Subject<IRegistredView>();
    public Subject<IRegistredView> ViewWasDeactivated { get; } = new Subject<IRegistredView>();
    public TView View { get; set; }

    private TViewModel viewModel;
    public TViewModel ViewModel
    {
        get { return viewModel; }
        set
        {
            if (value != viewModel)
            {
                viewModel = value;

                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        x => ViewModel.PropertyChanged += x,
                        x => ViewModel.PropertyChanged -= x)
                    .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) &&
                                ((IActivable) x.Sender).IsActive)
                    .Subscribe((x) => ViewWasActivated.OnNext(this));

                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                        x => ViewModel.PropertyChanged += x,
                        x => ViewModel.PropertyChanged -= x)
                    .Where(x => x.EventArgs.PropertyName == nameof(RegionViewModel<IView>.IsActive) &&
                                !((IActivable) x.Sender).IsActive)
                    .Subscribe((x) => ViewWasDeactivated.OnNext(this));
            }
        }
    }

    public Guid Guid { get; }

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

    public static string GetViewName()
    {
        return typeof(TView).Name + "_" + typeof(TViewModel).Name;
    }
}

public interface IRegistredView
{
    Subject<IRegistredView> ViewWasActivated { get; }
    Subject<IRegistredView> ViewWasDeactivated { get; }
    Guid Guid { get; }
    string ViewName { get; set; }


    void Activate();
    void Deactivate();
    
}

