using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Mvvm;
using PropertyChanged;

namespace VPlayer.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public abstract class ViewModel : BindableBase, IViewModel
    {
    }

    public abstract class ViewModel<TModel> : ViewModel, IParametrizedViewModel
    {
        public ViewModel(TModel model)
        {
            Model = model;
        }

        public TModel Model { get; set; }
    }

    public interface IViewModel { }
    public interface IParametrizedViewModel { }
}
