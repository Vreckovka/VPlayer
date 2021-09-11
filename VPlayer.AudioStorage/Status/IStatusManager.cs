using System;

namespace VPlayer.Core.Managers.Status
{
  public interface IStatusManager
  {
    StatusMessageViewModel ActualMessageViewModel { get; }

    IObservable<StatusMessageViewModel> OnStatusMessageUpdated { get; }

    void UpdateMessage(StatusMessageViewModel statusMessageViewModel);

    void UpdateMessageAndIncreaseProcessCount(StatusMessageViewModel statusMessageViewModel, int count = 1);
  }
}