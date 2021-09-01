using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace VPlayer.Core.Managers.Status
{
  public class StatusManager : ViewModel, IStatusManager
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private ReplaySubject<StatusMessage> onStatusMessageUpdatedSubject = new ReplaySubject<StatusMessage>(1);

    #endregion

    #region Constructors

    public StatusManager(IEventAggregator eventAggregator)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion

    #region Properties

    #region ActualMessage

    private StatusMessage actualMessage;

    public StatusMessage ActualMessage
    {
      get { return actualMessage; }
      private set
      {
        if (value != actualMessage)
        {
          actualMessage = value;
          RaisePropertyChanged();
        }
      }
    }



    #endregion

    #region OnStatusMessageUpdated

    public IObservable<StatusMessage> OnStatusMessageUpdated
    {
      get
      {
        return onStatusMessageUpdatedSubject.AsObservable();
      }
    }

    #endregion


    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusEvent).DisposeWith(this);
      onStatusMessageUpdatedSubject.DisposeWith(this);
    }

    #endregion

    #region OnStatusEvent

    private void OnStatusEvent(StatusMessage statusMessage)
    {
      UpdateMessage(statusMessage);
    }

    #endregion

    #region UpdateMessage

    public void UpdateMessage(StatusMessage statusMessage)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (ActualMessage == null || ActualMessage.Id != statusMessage.Id)
        {
          ActualMessage = statusMessage;
        }
        else
        {
          ActualMessage.Update(statusMessage);
        }

        CheckMessage(ActualMessage);

        onStatusMessageUpdatedSubject.OnNext(ActualMessage);
      });
    }

    #endregion


    public void UpdateMessageAndIncreaseProcessCount(StatusMessage statusMessage, int count = 1)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        statusMessage.ProcessedCount++;

        UpdateMessage(statusMessage);
      });
    }

    #endregion

    private void CheckMessage(StatusMessage statusMessage)
    {
      if (statusMessage.ProcessedCount == statusMessage.NumberOfProcesses && statusMessage.MessageStatusState != MessageStatusState.Failed)
      {
        statusMessage.MessageStatusState = MessageStatusState.Done;
      }
    }
  }
}
