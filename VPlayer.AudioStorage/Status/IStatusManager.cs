using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Prism.Events;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Controls.StatusMessage;

namespace VPlayer.Core.Managers.Status
{
  public interface IStatusManager
  {
    StatusMessageViewModel ActualMessageViewModel { get; }

    IObservable<StatusMessageViewModel> OnStatusMessageUpdated { get; }

    void UpdateMessage(StatusMessageViewModel statusMessageViewModel);

    void UpdateMessageAndIncreaseProcessCount(StatusMessageViewModel statusMessageViewModel, int count = 1);

    void ShowDoneMessage(string text, bool isPinned = false);

    void ShowErrorMessage(string text, bool isPinned = false);

    void ShowErrorMessage(Exception ex, bool isPinned = false);

    void ShowFailedMessage(string text, bool isPinned = false);
  }

  public class BaseStatusManager : ViewModel, IStatusManager
  {
    #region Fields

    protected readonly IEventAggregator eventAggregator;
    protected ReplaySubject<StatusMessageViewModel> onStatusMessageUpdatedSubject = new ReplaySubject<StatusMessageViewModel>(1);
    protected Subject<StatusMessageViewModel> onUpdateMessage = new Subject<StatusMessageViewModel>();

    #endregion

    #region Constructors

    public BaseStatusManager(IEventAggregator eventAggregator)
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

          if (actualMessageViewModel != null)
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe((x) =>
            {
              actualMessageViewModel.MessageState = MessageStatusState.Closed;
            }).DisposeWith(this);

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

      onUpdateMessage.Subscribe(OnUpdateMessage).DisposeWith(this);
      eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusEvent).DisposeWith(this);
    }

    #endregion

    #region OnStatusEvent

    protected void OnStatusEvent(StatusMessageViewModel statusMessageViewModel)
    {
      UpdateMessage(statusMessageViewModel);
    }

    #endregion

    #region OnUpdateMessage

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private StatusMessageViewModel lastProcessedMessage;
    protected async void OnUpdateMessage(StatusMessageViewModel statusMessageViewModel)
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        if (statusMessageViewModel != null)
          CheckMessage(statusMessageViewModel);

        ActualMessageViewModel = statusMessageViewModel;
        ActualMessageViewModel?.RaisePropertyChanges();
        onStatusMessageUpdatedSubject.OnNext(ActualMessageViewModel);

        if (statusMessageViewModel != null)
        {
          if (lastProcessedMessage != null && lastProcessedMessage.OriginalMessageId == ActualMessageViewModel.OriginalMessageId)
          {
            await Task.Delay(0);
          }
          else if (ActualMessageViewModel != null &&
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
      messages.Add(statusMessageViewModel);
      onUpdateMessage.OnNext(statusMessageViewModel);
    }

    #endregion

    #endregion

    #region UpdateMessageAndIncreaseProcessCount

    public void UpdateMessageAndIncreaseProcessCount(StatusMessageViewModel statusMessageViewModel, int count = 1)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        statusMessageViewModel.ProcessedCount++;

        UpdateMessage(statusMessageViewModel);
      });
    }

    #endregion

    public void ShowDoneMessage(string text, bool isPinned = false)
    {
      UpdateMessage(SingleMessage(text, StatusType.Done, isPinned));
    }

    public void ShowErrorMessage(string text, bool isPinned = false)
    {
      UpdateMessage(SingleMessage(text, StatusType.Error, isPinned));
    }

    public void ShowErrorMessage(Exception ex, bool isPinned = false)
    {
      UpdateMessage(SingleMessage(ex.ToString(), StatusType.Error, isPinned));
    }

    public void ShowFailedMessage(string text, bool isPinned = false)
    {
      UpdateMessage(SingleMessage(text, StatusType.Failed, isPinned));
    }

    private StatusMessageViewModel SingleMessage(string text, StatusType statusType, bool isPinned = false)
    {
      return new StatusMessageViewModel(1)
      {
        Message = text,
        Status = statusType,
        ProcessedCount = 1,
        IsPinned = isPinned
      };
    }

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