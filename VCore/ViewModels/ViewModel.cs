using Ninject;
using Prism.Mvvm;
using PropertyChanged;
using System;
using System.ComponentModel;

namespace VCore.ViewModels
{
  public interface IParametrizedViewModel
  {
  }

  public interface IViewModel : IInitializable, IDisposable, INotifyPropertyChanged
  {
  }

  [AddINotifyPropertyChangedInterface]
  public abstract class ViewModel : BindableBase, IViewModel
  {
    #region Methods

    public void Dispose()
    {
    }

    public virtual void Initialize()
    {
    }

    #endregion Methods
  }

  public abstract class ViewModel<TModel> : ViewModel, IParametrizedViewModel
  {
    #region Constructors

    public ViewModel(TModel model)
    {
      Model = model;
    }

    #endregion Constructors

    #region Properties

    public TModel Model { get; set; }

    #endregion Properties
  }
}