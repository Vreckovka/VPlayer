using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Prism.Events;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.Controls.StatusMessage;

namespace VPlayer.Core.Managers.Status
{
  public class VPlayerStatusManager : BaseStatusManager
  {
    public VPlayerStatusManager(IEventAggregator eventAggregator) : base(eventAggregator)
    {

    }

    public override void Initialize()
    {
      onStatusMessageUpdatedSubject.DisposeWith(this);

      onUpdateMessage.ObserveOn(Application.Current.Dispatcher).Subscribe(OnUpdateMessage).DisposeWith(this);

      eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusEvent).DisposeWith(this);
    }
  }
}
