using System;
using System.ComponentModel;
using VCore.Modularity.Interfaces;

namespace VCore.Modularity.RegionProviders
{
  public interface IRegionProvider
  {
    Guid RegisterView<TView, TViewModel>(string regionName, TViewModel viewModel, bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, INotifyPropertyChanged;


    void ActivateView(Guid guidObject);
    void DectivateView(Guid guidObject);
  }
}
