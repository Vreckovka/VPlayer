using System.ComponentModel;

namespace VCore.Modularity.Interfaces
{
  public interface IActivable : INotifyPropertyChanged
  {
    bool IsActive { get; set; }
  }
}
