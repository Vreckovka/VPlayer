using System;
using VCore.Interfaces.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels.Albums;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface IAlbumsViewModel : ICollectionViewModel<AlbumViewModel, Album>, INavigationItem
  {
  }
}
