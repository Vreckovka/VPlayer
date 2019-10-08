using System;
using System.ComponentModel;
using VCore.Modularity.Interfaces;

namespace VCore.Modularity.RegionProviders
{
  public interface IRegionProvider
  {
    #region Methods

    void ActivateView(Guid guidObject);

    void DectivateView(Guid guidObject);

    Guid RegisterView<TView, TViewModel>(
      string regionName,
      TViewModel viewModel,
      bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged;

    void GoBack(Guid guid);

    #endregion Methods
  }
}