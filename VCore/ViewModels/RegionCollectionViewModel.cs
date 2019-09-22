using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VCore.Annotations;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels.Navigation;

namespace VCore.ViewModels
{
    public class SelfActivableNavigationItem : ViewModel, INavigationItem
    {
        private readonly IRegionProvider regionProvider;


        public SelfActivableNavigationItem([NotNull] IRegionProvider regionProvider, Guid guid)
        {
            this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
            Guid = guid;
        }

        #region IsActive

        private bool isActive;
        private bool wasActivated;
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (value != isActive)
                {
                    isActive = value;

                    if (isActive)
                    {
                        regionProvider.ActivateView(Guid);
                    }
                    else
                    {
                        regionProvider.DectivateView(Guid);
                    }

                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        public string Header { get; set; }
        public Guid Guid { get; }
    }

    public abstract class RegionCollectionViewModel : ViewModel
    {
        private readonly IRegionProvider regionProvider;
        private Guid guid1;
        private Guid guid2;

        public RegionCollectionViewModel(IRegionProvider regionProvider)
        {
            this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
        }

        public abstract Dictionary<Type, Tuple<string, bool>> RegistredViews { get; set; }

        public Dictionary<Type, SelfActivableNavigationItem> Views { get; } = new Dictionary<Type, SelfActivableNavigationItem>();

        public override void Initialize()
        {
            base.Initialize();

            foreach (var registredView in RegistredViews)
            {
                var method = regionProvider.GetType().GetMethod("RegisterView");

                var genericMethod = method.MakeGenericMethod(registredView.Key, typeof(RegionCollectionViewModel));

                var result = genericMethod.Invoke(regionProvider, new object[] { registredView.Value.Item1, this, registredView.Value.Item2 });

                Views.Add(registredView.Key, new SelfActivableNavigationItem(regionProvider,(Guid)result));
            }
        }

        public void ActivateView(Type viewType)
        {
            Views[viewType].IsActive = true;
        }
    }

    public abstract class RegionViewModel<TView> : ViewModel, IRegionViewModel where TView : class, IView
    {
        #region Fields

        protected readonly IRegionProvider regionProvider;
        
        #endregion

        #region Constructors

        public RegionViewModel(IRegionProvider regionProvider)
        {
            this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
        }

        #endregion

        #region Properties

        public abstract string RegionName { get; }
        public abstract bool ContainsNestedRegions { get; }

        #region IsActive

        private bool isActive;
        private bool wasActivated;
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (value != isActive)
                {
                    isActive = value;

                    if (isActive)
                    {
                        OnActivation(!wasActivated);

                        if (!wasActivated)
                        {
                            wasActivated = true;
                        }

                    }

                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Initialize

        public override void Initialize()
        {
            base.Initialize();

            regionProvider.RegisterView<TView, RegionViewModel<TView>>(RegionName, this, ContainsNestedRegions);
        }

        #endregion

        #region OnActivation

        public virtual void OnActivation(bool firstActivation)
        {
        }

        #endregion 

        #endregion
    }

    public interface IRegionViewModel : IActivable
    {
        string RegionName { get; }
        bool ContainsNestedRegions { get; }
    }


}
