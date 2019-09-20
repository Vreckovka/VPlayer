using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Modularity.Interfaces;

namespace VCore.ViewModels.Navigation
{
  public interface INavigationItem : IActivable
  {
    string Header { get; }
  }
}
