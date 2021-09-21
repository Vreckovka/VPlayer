using System;

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

    void ShowFailedMessage(string text, bool isPinned = false);
  }
}