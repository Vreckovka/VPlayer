﻿using Prism.Regions;
using VCore.WPF.Interfaces.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface IArtistsViewModel : ICollectionViewModel<ArtistViewModel, Artist>, INavigationItem
  {
   
  }
}
