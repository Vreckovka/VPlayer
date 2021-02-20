using VCore.Interfaces.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels.TvShow;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface ITvShowsViewModel : ICollectionViewModel<TvShowViewModel, TvShow>, INavigationItem
  {
  }
}