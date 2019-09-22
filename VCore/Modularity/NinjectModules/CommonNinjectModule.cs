using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.RegionProviders;

namespace VCore.Modularity.NinjectModules
{
  public class CommonNinjectModule : BaseNinjectModule
  {
    public override void RegisterProviders()
    {
      Kernel.Bind<IRegionProvider>().To<BaseRegionProvider>().InSingletonScope();
    }

    public override void RegisterFactories()
    {
      Kernel.Bind<IViewModelsFactory>().To<BaseViewModelsFactory>();
      Kernel.Bind<IViewFactory>().To<BaseViewFactory>();
    }
  }
}
