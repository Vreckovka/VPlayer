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
  public class StatusManager : ViewModel, IStatusManager
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;
    private ReplaySubject<StatusMessageViewModel> onStatusMessageUpdatedSubject = new ReplaySubject<StatusMessageViewModel>(1);
    private Subject<StatusMessageViewModel> onUpdateMessage = new Subject<StatusMessageViewModel>();

    #endregion

    #region Constructors

    public StatusManager(IEventAggregator eventAggregator)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion

    #region Properties

    #region ActualMessage

    private StatusMessageViewModel actualMessageViewModel;

    public StatusMessageViewModel ActualMessageViewModel
    {
      get { return actualMessageViewModel; }
      private set
      {
        if (value != actualMessageViewModel)
        {
          actualMessageViewModel = value;
          RaisePropertyChanged();
        }
      }
    }



    #endregion

    #region OnStatusMessageUpdated

    public IObservable<StatusMessageViewModel> OnStatusMessageUpdated
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

      onStatusMessageUpdatedSubject.DisposeWith(this);

      onUpdateMessage.ObserveOn(Application.Current.Dispatcher).Subscribe(OnUpdateMessage).DisposeWith(this);
      eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusEvent).DisposeWith(this);
    }

    #endregion

    #region OnStatusEvent

    private void OnStatusEvent(StatusMessageViewModel statusMessageViewModel)
    {
      UpdateMessage(statusMessageViewModel);
    }

    #endregion

    #region OnUpdateMessage

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private StatusMessageViewModel lastProcessedMessage;
    private async void OnUpdateMessage(StatusMessageViewModel statusMessageViewModel)
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        if (statusMessageViewModel != null)
          CheckMessage(statusMessageViewModel);

        ActualMessageViewModel = statusMessageViewModel;
        onStatusMessageUpdatedSubject.OnNext(ActualMessageViewModel);

        if (statusMessageViewModel != null)
        {
          if (lastProcessedMessage != null && lastProcessedMessage.OriginalMessageId == ActualMessageViewModel.OriginalMessageId)
          {
            await Task.Delay(0);
          }
          else if(ActualMessageViewModel != null &&
                  ActualMessageViewModel.ProcessedCount != 0 && 
                  ActualMessageViewModel.Status != StatusType.Processing)
            await Task.Delay(3000);
        }

        lastProcessedMessage = ActualMessageViewModel;
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    #endregion

    #region UpdateMessage

    private List<StatusMessageViewModel> messages = new List<StatusMessageViewModel>();
  

    public void UpdateMessage(StatusMessageViewModel statusMessageViewModel)
    {
      var messageCopy = statusMessageViewModel.Copy();
      messageCopy.OriginalMessageId = statusMessageViewModel.Id;

      messages.Add(messageCopy);
      onUpdateMessage.OnNext(messageCopy);
    }

    #endregion

    #endregion

    #region UpdateMessageAndIncreaseProcessCount

    public void UpdateMessageAndIncreaseProcessCount(StatusMessageViewModel statusMessageViewModel, int count = 1)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        statusMessageViewModel.ProcessedCount++;

        UpdateMessage(statusMessageViewModel);
      });
    }

    #endregion

    #region CheckMessage

    private void CheckMessage(StatusMessageViewModel statusMessageViewModel)
    {
      if (statusMessageViewModel.ProcessedCount == statusMessageViewModel.NumberOfProcesses &&
          statusMessageViewModel.Status != StatusType.Failed)
      {
        statusMessageViewModel.Status = StatusType.Done;
      }
    }

    #endregion

  }
}
