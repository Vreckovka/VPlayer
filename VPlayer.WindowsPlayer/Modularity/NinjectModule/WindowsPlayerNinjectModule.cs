using System.Diagnostics;
using VCore.Modularity.Interfaces;
using VCore.Modularity.NinjectModules;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Library.ViewModels;
using VPlayer.Library.Views;
using VPlayer.WindowsPlayer.ViewModels;
using VPlayer.WindowsPlayer.Views;

namespace VPlayer.WindowsPlayer.Modularity.NinjectModule
{
  public class WindowsPlayerNinjectModule : BaseNinjectModule
  {
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
