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
    public PlaySongsEventData(
      IEnumerable<SongInPlayList> songs,
      PlaySongsAction playSongsAction, 
      object model)

    {
      Songs = songs;
      PlaySongsAction = playSongsAction;
      Model = model;
    }
    public PlaySongsEventData(
      IEnumerable<SongInPlayList> songs,
      PlaySongsAction playSongsAction, 
      bool isShufle, 
      bool isRepeat,
      float? setPostion,
      object model)
    {
      PlaySongsAction = playSongsAction;
      Songs = songs;
      IsShufle = isShufle;
      IsRepeat = isRepeat;
      SetPostion = setPostion;
      Model = model;
   
    }

    public IEnumerable<SongInPlayList> Songs { get;  }
    public PlaySongsAction PlaySongsAction { get; }
    public bool IsShufle { get;  }
    public bool IsRepeat { get;  }
    public float? SetPostion { get;  }
    public object Model { get; }

    #region GetData

    public TData GetModel<TData>() where TData : class
    {
      if (Model is TData data)
      {
        return data;
      }

      return null;
    } 

    #endregion
  }

  public class PlaySongsEvent : PubSubEvent<PlaySongsEventData>
  {
  }


}