using Ninject.Modules;
using Prism.Ioc;
using Prism.Modularity;
using VCore.Factories;
using VCore.Modularity.RegionProviders;

namespace VCore.Modularity.NinjectModules
{
  public class BaseNinjectModule : NinjectModule
  {
    public override void Load()
    {
      RegisterProviders();
      RegisterFactories();
      RegisterViewModels();
      RegisterViews();
    }

    public virtual void RegisterProviders()
    {
    }

    public virtual void RegisterFactories()
    {
    }

    public virtual void RegisterViews()
    {
    }

    public virtual void RegisterViewModels() { }
  }
}
