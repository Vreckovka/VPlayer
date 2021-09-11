using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using Prism.Events;
using VCore;
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

      eventAggregator.GetEvent<StatusMessageEvent>().Subscribe(OnStatusEvent).DisposeWith(this);
      onStatusMessageUpdatedSubject.DisposeWith(this);
    }

    #endregion

    #region OnStatusEvent

    private void OnStatusEvent(StatusMessageViewModel statusMessageViewModel)
    {
      UpdateMessage(statusMessageViewModel);
    }

    #endregion

    #region UpdateMessage

    public void UpdateMessage(StatusMessageViewModel statusMessageViewModel)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        if (ActualMessageViewModel == null || ActualMessageViewModel.Id != statusMessageViewModel.Id)
        {
          ActualMessageViewModel = statusMessageViewModel;
        }
        else
        {
          ActualMessageViewModel.Update(statusMessageViewModel);
        }

        CheckMessage(ActualMessageViewModel);

        onStatusMessageUpdatedSubject.OnNext(ActualMessageViewModel);
      });
    }

    #endregion


    public void UpdateMessageAndIncreaseProcessCount(StatusMessageViewModel statusMessageViewModel, int count = 1)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        statusMessageViewModel.ProcessedCount++;

        UpdateMessage(statusMessageViewModel);
      });
    }

    #endregion

    private void CheckMessage(StatusMessageViewModel statusMessageViewModel)
    {
      if (statusMessageViewModel.ProcessedCount == statusMessageViewModel.NumberOfProcesses && 
          statusMessageViewModel.Status != StatusType.Failed)
      {
        statusMessageViewModel.Status = StatusType.Done;
      }
    }
  }
}
