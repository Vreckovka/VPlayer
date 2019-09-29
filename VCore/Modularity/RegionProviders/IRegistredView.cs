using System;
using System.Reactive.Subjects;
using Prism.Regions;

namespace VCore.Modularity.RegionProviders
{
  public interface IRegistredView
  {
    Subject<IRegistredView> ViewWasActivated { get; }
    Subject<IRegistredView> ViewWasDeactivated { get; }
    Guid Guid { get; }
    string ViewName { get; set; }
    IRegion Region { get; set; }

    void Activate();
    void Deactivate();

  }
}