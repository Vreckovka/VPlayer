using System;
using VCore.Modularity.Interfaces;
using VCore.ViewModels;

namespace VCore.Modularity.RegionProviders
{
  public interface IRegionProvider
  {
    void RegisterView<TView, TViewModel>(string name, TViewModel viewModel, bool containsNestedRegion)
      where TView : class, IView
      where TViewModel : class, IRegionViewModel<TView>;
  }
}
