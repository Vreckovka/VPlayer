using System.Data.Common;
using Ninject;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.Player.ViewModels;
using VPlayer.WebPlayer.ViewModels;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Modularity.NinjectModules
{
  public class VPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      if (Kernel != null)
      {
        Kernel.Load<VPlayerCoreModule>();
        Kernel.Load<WindowsPlayerNinjectModule>();


        Kernel.BindToSelfInSingletonScope<WindowsPlayerViewModel>();
        Kernel.BindToSelfInSingletonScope<WebPlayerViewModel>();
        Kernel.BindToSelfInSingletonScope<PlayerViewModel>();

          //< DbProviderFactories >
          //< remove invariant = "System.Data.SQLite.EF6" />
 
          //< add name = "SQLite Data Provider (Entity Framework 6)" invariant = "System.Data.SQLite.EF6" description = ".NET Framework Data Provider for SQLite (Entity Framework 6)" type = "System.Data.SQLite.EF6.SQLiteProviderFactory, System.Data.SQLite.EF6" />
        
          //< remove invariant = "System.Data.SQLite" />< add name = "SQLite Data Provider" invariant = "System.Data.SQLite" description = ".NET Framework Data Provider for SQLite" type = "System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
                  
          //</ DbProviderFactories >




      }
    }
  }
}
