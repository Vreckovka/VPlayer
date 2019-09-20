using System;
using System.ComponentModel;
using Ninject;
using Prism.Mvvm;
using PropertyChanged;

namespace VCore.ViewModels
{
  [AddINotifyPropertyChangedInterface]
  public abstract class ViewModel : BindableBase, IViewModel
  {
    public virtual void Initialize()
    {
      ;
    }

    public void Dispose()
    {
    }
  }

  public abstract class ViewModel<TModel> : ViewModel, IParametrizedViewModel
  {
    public ViewModel(TModel model)
    {
      Model = model;
    }

    public TModel Model { get; set; }
  }

  public interface IViewModel : IInitializable, IDisposable, INotifyPropertyChanged
  {

  }
  public interface IParametrizedViewModel { }
}
