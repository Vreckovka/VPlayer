using Prism.Events;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public enum PlaySongsAction
  {
    Play,
    Add,
    PlayFromPlaylist,
    PlayFromPlaylistLast,
  }

  public class PlaySongsEventData
  {
    public IEnumerable<SongInPlayList> Songs { get; set; }
    public PlaySongsAction PlaySongsAction { get; set; }
    public bool IsShufle { get; set; }
    public bool IsRepeat { get; set; }
    public float? SetPostion { get; set; }
    public int IdModel { get; set; }
  }

  public class PlaySongsEvent : PubSubEvent<PlaySongsEventData>
  {
  }


}