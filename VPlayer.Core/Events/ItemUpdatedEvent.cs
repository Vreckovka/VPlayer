using System;
using System.Collections.Generic;
using System.Text;
using Prism.Events;

namespace VPlayer.Core.Events
{
  public class ItemUpdatedEventArgs<TModel>
  {
    public TModel Model { get; set; }
  }

  public class ItemUpdatedEvent<TModel> : PubSubEvent<ItemUpdatedEventArgs<TModel>>
  {
  }
}
