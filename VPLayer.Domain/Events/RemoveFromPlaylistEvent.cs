using Prism.Events;
using System.Collections.Generic;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public enum DeleteType
  {
    SingleFromPlaylist,
    AlbumFromPlaylist
  }
  public class RemoveFromPlaylistEventArgs<TModel>
  {
    public DeleteType DeleteType { get; set; }
    public List<TModel> ItemsToRemove { get; set; }
  }

  public class RemoveFromPlaylistEvent<TModel> : PubSubEvent<RemoveFromPlaylistEventArgs<TModel>>
  {

  }
}