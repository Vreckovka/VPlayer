using VCore.WPF.Interfaces.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.Interfaces.ViewModels
{
  public interface ITvShowsViewModel : ICollectionViewModel<TvShowViewModel, TvShow>, INavigationItem
  {
  }
}