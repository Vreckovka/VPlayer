using System.Collections.Generic;

namespace VPlayer.Core.Events
{

  public interface IPlayItemsEventData<TEventData>
  {
    IEnumerable<TEventData> Items { get; }
    EventAction EventAction { get; }
    bool? IsShuffle { get; }
    bool? IsRepeat { get; }
    float? SetPostion { get; }
    object Model { get; }

    public bool SetItemOnly { get; set; }

    TData GetModel<TData>() where TData : class;

    bool StorePlaylist { get; }
  }

  public class PlayItemsEventData<TEventData> : IPlayItemsEventData<TEventData>
  {
    public PlayItemsEventData(
      IEnumerable<TEventData> items,
      EventAction eventAction,
      object model)
    {
      Items = items;
      EventAction = eventAction;
      Model = model;
    }

    public PlayItemsEventData(
      IEnumerable<TEventData> items,
      EventAction eventAction,
      bool? isShuffle,
      bool? isRepeat,
      float? setPostion,
      object model)
    {
      EventAction = eventAction;
      Items = items;
      IsShuffle = isShuffle;
      IsRepeat = isRepeat;
      SetPostion = setPostion;
      Model = model;

    }


    public IEnumerable<TEventData> Items { get; }
    public EventAction EventAction { get; }
    public bool? IsShuffle { get; }
    public bool? IsRepeat { get; }
    public float? SetPostion { get; }
    public object Model { get; }
    public bool StorePlaylist { get; set; } = true;
    public bool SetItemOnly { get; set; }

    #region GetModel

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
}