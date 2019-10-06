using Ninject;
using VCore.Modularity.NinjectModules;
using VPlayer.Library.Modularity.NinjectModule;

namespace VPlayer.WindowsPlayer.Modularity.NinjectModule
{
  public class WindowsPlayerNinjectModule : BaseNinjectModule
  {
    #region Methods

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

    #endregion Methods
  }
}