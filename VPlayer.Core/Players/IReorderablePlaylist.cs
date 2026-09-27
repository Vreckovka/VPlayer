using System.Threading.Tasks;

namespace VPlayer.Core.ViewModels
{
  public interface IReorderablePlaylist
  {
    bool CanReorderPlaylist { get; }
    int PlaylistItemIndex(object item);
    Task<bool> MovePlaylistItemAsync(object item, int newIndex);
  }
}
