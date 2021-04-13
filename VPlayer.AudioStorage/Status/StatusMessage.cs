using System;
using VCore.Standard.Common;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Managers.Status
{
  public class StatusMessage : VBindableBase, IUpdateable<StatusMessage>
  {
    public StatusMessage(int numberOfProcesses)
    {
      if (numberOfProcesses <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfProcesses));

      Id = Guid.NewGuid();
      NumberOfProcesses = numberOfProcesses;
    }

    #region Properties

    public Guid Id { get; }


    #region NumberOfProcesses

    private int bumberOfProcesses;

    public int NumberOfProcesses
    {
      get { return bumberOfProcesses; }
      set
      {
        if (value != bumberOfProcesses)
        {
          bumberOfProcesses = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Process

    private double process;

    public double Process
    {
      get { return process; }
      private set
      {
        if (value != process)
        {
          process = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ProcessedCount

    private int processedCount;

    public int ProcessedCount
    {
      get { return processedCount; }
      set
      {
        if (value != processedCount)
        {
          processedCount = value;

          Process = ProcessedCount * 100.0 / NumberOfProcesses;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Message

    private string message;

    public string Message
    {
      get { return message; }
      set
      {
        if (value != message)
        {
          message = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualStatus

    private MessageStatusState actualMessageStatusState;

    public MessageStatusState ActualMessageStatusState
    {
      get { return actualMessageStatusState; }
      set
      {
        if (value != actualMessageStatusState)
        {
          actualMessageStatusState = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FailedMessage

    private string failedMessage;

    public string FailedMessage
    {
      get { return failedMessage; }
      set
      {
        if (value != failedMessage)
        {
          failedMessage = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Update

    public void Update(StatusMessage other)
    {
      ProcessedCount = other.ProcessedCount;
      
      if(other.NumberOfProcesses == 1)
      {
        Process = other.Process;
      }
      else
      {
        Process = ProcessedCount * 100.0 / NumberOfProcesses;
      }

      Message = other.Message;

      ActualMessageStatusState = other.ActualMessageStatusState;

      if (ProcessedCount == NumberOfProcesses && ActualMessageStatusState != MessageStatusState.Failed)
      {
        Message += " is DONE";
        ActualMessageStatusState = MessageStatusState.Done;
      }

      
      FailedMessage = other.FailedMessage;
    }

    #endregion 

    #endregion
  }
}