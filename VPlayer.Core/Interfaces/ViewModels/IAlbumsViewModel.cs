using VCore.Interfaces.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface IAlbumsViewModel : ICollectionViewModel<AlbumViewModel, Album>, INavigationItem
  {
     
  }
}
