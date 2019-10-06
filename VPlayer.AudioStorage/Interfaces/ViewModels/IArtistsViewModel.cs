using VCore.Interfaces.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface IArtistsViewModel : ICollectionViewModel<ArtistViewModel>, INavigationItem
  {
  }
}
