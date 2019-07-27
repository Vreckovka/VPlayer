using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Modularity;

namespace VPlayer.Core.ViewModels
{
    public class ModuleViewModel : ViewModel, IModule
    {
        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public virtual void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}
