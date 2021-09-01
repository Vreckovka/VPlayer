using System;
using System.Windows.Input;
using VCore;
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

    private MessageStatusState messageStatusState;

    public MessageStatusState MessageStatusState
    {
      get { return messageStatusState; }
      set
      {
        if (value != messageStatusState)
        {
          messageStatusState = value;
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

    #region IsForcedClose

    private bool isForcedClose;

    public bool IsForcedClose
    {
      get { return isForcedClose; }
      set
      {
        if (value != isForcedClose)
        {
          isForcedClose = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OnClose

    #region ChoosePath

    private ActionCommand close;

    public ICommand Close
    {
      get
      {
        if (close == null)
        {
          close = new ActionCommand(OnClose);
        }

        return close;
      }
    }

    public void OnClose()
    {
      IsForcedClose = !IsForcedClose;
    }

    #endregion

    #endregion

    #endregion

    #region Methods

    #region Update

    public void Update(StatusMessage other)
    {
      ProcessedCount = other.ProcessedCount;

      if (other.NumberOfProcesses == 1)
      {
        Process = other.Process;
      }
      else
      {
        Process = ProcessedCount * 100.0 / NumberOfProcesses;
      }

      Message = other.Message;

      MessageStatusState = other.MessageStatusState;

      if (ProcessedCount == NumberOfProcesses && MessageStatusState != MessageStatusState.Failed)
      {
        MessageStatusState = MessageStatusState.Done;
      }


      FailedMessage = other.FailedMessage;
    }

    #endregion 

    #endregion
  }
}