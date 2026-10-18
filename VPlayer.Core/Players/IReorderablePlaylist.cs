using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPlayer.Core.ViewModels
{
  public interface IReorderablePlaylist
  {
    bool CanReorderPlaylist { get; }
    int PlaylistItemIndex(object item);
    int[] PlaylistItemIndices(IEnumerable<object> items);
    Task<bool> MovePlaylistItemAsync(object item, int newIndex);
    Task<bool> MovePlaylistItemsAsync(IEnumerable<object> items, int insertionIndex);
  }
}
