using VCore.Standard.Helpers;

namespace VPlayer.Core.ViewModels
{
  public static class PlaylistSaveSnapshot
  {
    public static TPlaylist Create<TPlaylist>(TPlaylist playlist) => ReferenceEquals(playlist, null) ? default : playlist.DeepClone();
  }
}
