using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using VCore.Modularity.NinjectModules;
using VPlayer.AudioStorage.Modularity.NinjectModules;

namespace VPlayer.Library.Modularity.NinjectModule
{
    public class LibraryNinjectModule : BaseNinjectModule
    {
        public override void Load()
        {
            base.Load();
            Kernel.Load<AudioStorageNinjectModule>();
        }

        public override void RegisterViewModels()
        {
            base.RegisterViewModels();

           
        }
    }
}
