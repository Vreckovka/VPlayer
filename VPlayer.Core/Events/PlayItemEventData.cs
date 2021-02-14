using System.Collections.Generic;

namespace VPlayer.Core.Events
{

  public interface IPlayItemEventData<TEventData>
  {
    IEnumerable<TEventData> Items { get; }
    EventAction EventAction { get; }
    bool? IsShufle { get; }
    bool? IsRepeat { get; }
    float? SetPostion { get; }
    object Model { get; }

    TData GetModel<TData>() where TData : class;
  }

  public class PlayItemEventData<TEventData> : IPlayItemEventData<TEventData>
  {
    public PlayItemEventData(
      IEnumerable<TEventData> items,
      EventAction eventAction,
      object model)

    {
      Items = items;
      EventAction = eventAction;
      Model = model;
    }

    public PlayItemEventData(
      IEnumerable<TEventData> items,
      EventAction eventAction,
      bool? isShufle,
      bool? isRepeat,
      float? setPostion,
      object model)
    {
      EventAction = eventAction;
      Items = items;
      IsShufle = isShufle;
      IsRepeat = isRepeat;
      SetPostion = setPostion;
      Model = model;

    }


    public IEnumerable<TEventData> Items { get; }
    public EventAction EventAction { get; }
    public bool? IsShufle { get; }
    public bool? IsRepeat { get; }
    public float? SetPostion { get; }
    public object Model { get; }


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