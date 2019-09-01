using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Extensions.Factory;
using Ninject.Modules;
using Prism.Ioc;
using Prism.Modularity;
using VPlayer.Core.Factories;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Modules
{
    public class BaseNinjectModule : NinjectModule, IModule
    {
        public override void Load()
        {
            Kernel.Bind<IViewModelFactory>().To<BaseViewModelFactory>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            ;
        }
    }
}
