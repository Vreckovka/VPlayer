﻿using System;
using System.Windows.Input;
using VCore;
using VCore.Standard.Common;
using VCore.WPF.Controls.StatusMessage;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Managers.Status
{
  public class StatusMessageViewModel : VBindableBase, IUpdateable<StatusMessageViewModel>
  {
    public StatusMessageViewModel(int numberOfProcesses)
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

    #region IsMinimized

#if DEBUG
    private bool isMinimized = false;
#else
   private bool isMinimized = true;
#endif



    public bool IsMinimized
    {
      get { return isMinimized; }
      set
      {
        if (value != isMinimized)
        {
          isMinimized = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsClosed

    private bool isClosed;

    public bool IsClosed
    {
      get { return isClosed; }
      set
      {
        if (value != isClosed)
        {
          isClosed = value;
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
      IsMinimized = !IsMinimized;
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
      IsClosed = !IsClosed;
    }

    #endregion


    #endregion

    #region Methods

    #region Update

    public void Update(StatusMessageViewModel other)
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

      Status = other.Status;

      if (ProcessedCount == NumberOfProcesses && 
          Status != StatusType.Error && 
          Status != StatusType.Failed)
      {
        Status = StatusType.Done;
      }

      IsClosed = other.IsClosed;
      isMinimized = other.IsMinimized;
    }

    #endregion

    #endregion
  }
}