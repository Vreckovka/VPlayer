using VCore.Modularity.Interfaces;
using VCore.ViewModels;

namespace VCore.Factories.Views
{
  public interface IViewFactory
  {
    TView Create<TView>();
  }

  public interface IViewFactory<out TView> 
    where TView : IView
  {
    TView Create();
  }

  public interface IViewFactory<out TView, in TViewModel> : IViewFactory<TView>
    where TViewModel : IViewModel
    where TView : IView
  {
    TView Create(TViewModel viewModel);
  }
}