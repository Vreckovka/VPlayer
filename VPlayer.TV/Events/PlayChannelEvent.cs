using System;
using System.Collections.Generic;
using System.Text;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.IPTV.Events
{
  public class PlayChannelEvent : PubSubEvent<ITvChannel>
  {
  }
}
