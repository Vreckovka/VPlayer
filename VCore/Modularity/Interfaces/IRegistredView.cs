using System;
using System.Reactive.Subjects;

namespace VCore.Modularity.Interfaces
{
  public interface IRegistredView
  {
    Subject<IRegistredView> ViewWasActivated { get; }
    Subject<IRegistredView> ViewWasDeactivated { get; }
    Guid Guid { get; }
    string ViewName { get; set; }

    void Activate();
    void Deactivate();

  }
}