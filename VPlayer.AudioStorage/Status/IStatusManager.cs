using System;

namespace VPlayer.Core.Managers.Status
{
  public interface IStatusManager
  {
    StatusMessage ActualMessage { get; }

    IObservable<StatusMessage> OnStatusMessageUpdated { get; }

    void UpdateMessage(StatusMessage statusMessage);
  }
}