using System;
using System.Collections.Generic;
using System.Windows.Input;
using VCore;
using VCore.Standard.Common;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Misc;

namespace VPlayer.Core.Managers.Status
{
  public class StatusMessageViewModel : VBindableBase
  {
    public StatusMessageViewModel(int numberOfProcesses)
    {
      if (numberOfProcesses <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfProcesses));

      Id = Guid.NewGuid();
      NumberOfProcesses = numberOfProcesses;


      
    }

    #region Properties

    public Guid Id { get; }
    public Guid OriginalMessageId { get; set; }

    #region ParentMessage

    private StatusMessageViewModel parentMessage;

    public StatusMessageViewModel ParentMessage
    {
      get { return parentMessage; }
      set
      {
        if (value != parentMessage)
        {
          parentMessage = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region MessageStatus

    private StatusType status;

    public StatusType Status
    {
      get { return status; }
      set
      {
        if (value != status)
        {
          status = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MessageState


#if DEBUG
    private MessageStatusState messageState = MessageStatusState.Open;
#else
   private MessageStatusState messageState = MessageStatusState.Open;
#endif
    public MessageStatusState MessageState
    {
      get { return messageState; }
      set
      {
        if (value != messageState)
        {
          messageState = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPinned

    private bool isPinned;

    public bool IsPinned
    {
      get { return isPinned; }
      set
      {
        if (value != isPinned)
        {
          isPinned = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public List<StatusMessageViewModel> SubItems { get; set; } = new List<StatusMessageViewModel>();

    #endregion

    #region Commands

    #region Minimized

    private ActionCommand minimize;

    public ICommand Minimize
    {
      get
      {
        if (minimize == null)
        {
          minimize = new ActionCommand(OnMinimized);
        }

        return minimize;
      }
    }

    public void OnMinimized()
    {
      if (MessageStatusState.Minimized == MessageState)
      {
        MessageState = MessageStatusState.Open;
      }
      else
      {
        MessageState = MessageStatusState.Minimized;
      }

    }

    #endregion

    #region Close

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
      MessageState = MessageStatusState.Closed;
    }

    #endregion


    #endregion

    #region Methods

    #region CopyParentState

    public void CopyParentState(StatusMessageViewModel statusMessageViewModel)
    {
      if(statusMessageViewModel != null)
      {
        MessageState = statusMessageViewModel.MessageState;
        IsPinned = statusMessageViewModel.IsPinned;
        ParentMessage = statusMessageViewModel;
      }
   
    }

    #endregion

    public StatusMessageViewModel Copy()
    {
      return new StatusMessageViewModel(NumberOfProcesses)
      {
        IsPinned = IsPinned,
        MessageState = MessageState,
        Message = Message,
        NumberOfProcesses = NumberOfProcesses,
        ProcessedCount = ProcessedCount,
        Process = Process,
        Status = Status,
        OriginalMessageId = OriginalMessageId
      };
    }

    #region ToString

    public override string ToString()
    {
      return Id + " " + Status + " " + Message + " " + ProcessedCount + "/" + NumberOfProcesses;
    }

    #endregion

    #endregion
  }
}