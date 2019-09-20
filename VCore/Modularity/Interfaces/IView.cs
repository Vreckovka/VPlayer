using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.ViewModels;

namespace VCore.Modularity.Interfaces
{
  public interface IView
  {
    object DataContext { get; set; }
  }

  public interface IView<TViewModel> : IView
    where TViewModel : IViewModel
  {
    TViewModel ViewModel { get; set; }
  }
}
