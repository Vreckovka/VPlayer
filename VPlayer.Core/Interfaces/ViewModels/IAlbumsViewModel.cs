﻿using VCore.WPF.Interfaces.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels.Albums;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface IAlbumsViewModel : ICollectionViewModel<AlbumViewModel, Album>, INavigationItem
  {
  }
}
