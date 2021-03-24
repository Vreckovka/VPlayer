using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using Ninject.Parameters;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.Factories
{
  public class VPlayerViewModelsFactory : BaseViewModelsFactory, IVPlayerViewModelsFactory
  {
    public VPlayerViewModelsFactory(IKernel kernel) : base(kernel)
    {
    }
  }
}
