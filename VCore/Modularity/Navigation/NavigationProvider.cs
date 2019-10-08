using Prism.Regions;
using System.Collections.Generic;
using VCore.Modularity.RegionProviders;

namespace VCore.Modularity.Navigation
{
  public interface INavigationProvider
  {
    #region Properties

    Dictionary<IRegion, NavigationSet> NavigationItems { get; }

    #endregion Properties

    #region Methods

    IRegistredView GetNext(IRegistredView registredView);

    IRegistredView GetPrevious(IRegistredView registredView);

    void SetNavigation(IRegistredView registredView, IRegion region = null);

    #endregion Methods
  }

  public class NavigationProvider : INavigationProvider
  {
    #region Constructors

    public NavigationProvider()
    {
    }

    #endregion Constructors

    #region Properties

    public Dictionary<IRegion, NavigationSet> NavigationItems { get; } = new Dictionary<IRegion, NavigationSet>();

    #endregion Properties

    #region Methods

    public IRegistredView GetNext(IRegistredView registredView)
    {
      if (NavigationItems.TryGetValue(registredView.Region, out var navigationItems))
      {
        return navigationItems.GetNext();
      }

      return null;
    }

    public IRegistredView GetPrevious(IRegistredView registredView)
    {
      if (NavigationItems.TryGetValue(registredView.Region, out var navigationItems))
      {
        return navigationItems.GetPrevious();
      }

      return null;
    }

    public void SetNavigation(IRegistredView registredView, IRegion region = null)
    {
      var requestedRegion = registredView.Region;

      if (region != null)
      {
        requestedRegion = region;
      }

      if (NavigationItems.TryGetValue(requestedRegion, out var navigationItems))
      {
        navigationItems.Add(registredView);
      }
      else
      {
        var list = new NavigationSet();
        list.Add(registredView);

        NavigationItems.Add(registredView.Region, list);
      }
    }

    #endregion Methods
  }

  public class NavigationSet
  {
    #region Fields

    private bool isInBackState;

    #endregion Fields

    #region Properties

    public LinkedListNode<IRegistredView> Actual { get; set; }
    public LinkedList<IRegistredView> Chain { get; } = new LinkedList<IRegistredView>();

    #endregion Properties

    #region Methods

    public void Add(IRegistredView registredView)
    {
      if (isInBackState)
      {
        RemoveAllNodesAfter(Actual);
        isInBackState = false;
      }

      Chain.AddLast(registredView);
      Actual = Chain.Last;
    }

    public IRegistredView GetNext()
    {
      if (Actual.Next == null)
        return null;

      Actual = Actual.Next;

      return Actual.Value;
    }

    public IRegistredView GetPrevious()
    {
      if (Actual.Previous == null)
        return null;

      Actual = Actual.Previous;
      isInBackState = true;

      return Actual.Value;
    }

    public void RemoveAllNodesAfter(LinkedListNode<IRegistredView> node)
    {
      while (node.Next != null)
        Chain.Remove(node.Next);
    }

    #endregion Methods
  }
}