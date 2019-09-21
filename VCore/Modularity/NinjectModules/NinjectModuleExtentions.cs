using Ninject;
using Ninject.Syntax;
using VCore.Factories.Views;
using VCore.Modularity.Interfaces;
using VCore.ViewModels;

namespace VCore.Modularity.NinjectModules
{
  public static class NinjectModuleExtentions
  {
    #region BindToSelfInSingletonScope

    public static IBindingOnSyntax<TInitializable> BindToSelfInSingletonScope<TInitializable>(this IKernel kernel)
      where TInitializable : IInitializable
    {
        return kernel.Bind<TInitializable>().ToSelf().InSingletonScope();
    }

    #endregion

    #region InitializeOnActivation

    public static IBindingOnSyntax<TInitializable> InitializeOnActivation<TInitializable>(this IBindingOnSyntax<TInitializable> kernelBinding)
      where TInitializable : IInitializable
    {
      return kernelBinding.OnActivation(x => x.Initialize());
    }

    #endregion

    #region BindToSelf

    public static IBindingWhenInNamedWithOrOnSyntax<TInitializable> BindToSelf<TInitializable>(this IKernel kernel)
      where TInitializable : IInitializable
    {
      return kernel.Bind<TInitializable>().ToSelf();
    }

    #endregion

    #region BindToSelfAndInitialize

    public static IBindingOnSyntax<TInitializable> BindToSelfAndInitialize<TInitializable>(this IKernel kernel)
      where TInitializable : IInitializable
    {
      return kernel.BindToSelf<TInitializable>().InTransientScope().InitializeOnActivation();
    }

    #endregion

    #region InitializeImmediately

    public static TInitializable InitializeImmediately<TInitializable>(this TInitializable initializable)
      where TInitializable : IInitializable
    {
      initializable.Initialize();

      return initializable;
    }

    #endregion
  }
}
