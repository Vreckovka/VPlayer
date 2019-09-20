using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCore.Modularity.Interfaces
{
  public interface IActivable : INotifyPropertyChanged
  {
    bool IsActive { get; set; }
  }
}
