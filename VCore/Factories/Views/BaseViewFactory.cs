using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;

namespace VCore.Factories.Views
{
  public class BaseViewFactory : IViewFactory
  {
    private readonly IKernel kernel;

    public BaseViewFactory(IKernel kernel)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    public TView Create<TView>()
    {
      return kernel.Get<TView>();
    }
  }
}
