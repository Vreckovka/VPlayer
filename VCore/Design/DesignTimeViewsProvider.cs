using System.Collections.Generic;
using System.Windows.Controls;
using VCore.Modularity.Interfaces;

namespace VCore.Design
{
  public abstract class DesignTimeViewsProvider
  {
    public IEnumerable<IView> GetViewForRegion(string regionName)
    {
      return OnGetViewForRegion(regionName);
    }

    protected abstract IEnumerable<IView> OnGetViewForRegion(string regionName);
  }
}