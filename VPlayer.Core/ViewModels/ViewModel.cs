using Prism.Mvvm;
using PropertyChanged;

namespace VPlayer.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ViewModel : BindableBase
    {
    }

    public class ViewModel<TModel> : ViewModel
    {
        public ViewModel(TModel model)
        {
            Model = model;
        }

        public TModel Model { get; set; }
    }
}
