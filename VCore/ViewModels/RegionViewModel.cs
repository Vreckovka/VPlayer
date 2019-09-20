using System;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;

namespace VCore.ViewModels
{
  public abstract class RegionViewModel<TView> : ViewModel, IRegionViewModel<TView> where TView : class, IView
  {
    protected readonly IRegionProvider regionProvider;

    public abstract string RegionName { get; }
    public abstract bool ContainsNestedRegions { get; }

    #region IsActive

    private bool isActive;
    private bool wasActivated;
    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;

          if (isActive)
          {
            OnActivation(!wasActivated);

            if (!wasActivated)
            {
              wasActivated = true;
            }

          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public RegionViewModel(IRegionProvider regionProvider)
    {
      this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
    }

    public override void Initialize()
    {
      base.Initialize();

      regionProvider.RegisterView<TView, RegionViewModel<TView>>(RegionName,this, ContainsNestedRegions);
    }

    public virtual void OnActivation(bool firstActivation)
    {
    }
  }

  public interface IRegionViewModel<in TView> : IActivable
  {
    string RegionName { get; }
    bool ContainsNestedRegions { get; }
  }
}
