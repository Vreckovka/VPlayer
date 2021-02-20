using Prism.Events;

namespace VPlayer.Core.Events
{
  public class ContentFullScreenEventArgs
  {
    public bool IsFullScreen { get; set; }
    public object View { get; set; }
  }

  public class ContentFullScreenEvent : PubSubEvent<ContentFullScreenEventArgs>
  {
  }
}