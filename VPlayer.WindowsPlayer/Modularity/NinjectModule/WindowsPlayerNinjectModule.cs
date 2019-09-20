using System.Diagnostics;
using Ninject;
using VCore.Modularity.Interfaces;
using VCore.Modularity.NinjectModules;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Library.Modularity.NinjectModule;
using VPlayer.Library.ViewModels;
using VPlayer.Library.Views;
using VPlayer.WindowsPlayer.ViewModels;
using VPlayer.WindowsPlayer.Views;

namespace VPlayer.WindowsPlayer.Modularity.NinjectModule
{
  public class WindowsPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      Kernel.Load<LibraryNinjectModule>();
    }
    public override void RegisterViewModels()
    {
      base.RegisterViewModels();
    }

    public override void RegisterViews()
    {
      base.RegisterViewModels();
    }
  }
}
