using Ninject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Logger
{
  public enum MessageType
  {
    Inform,
    Success,
    Error,
    Warning
  }

  public class Logger : ILogger
  {
    private readonly ILoggerContainer loggerContainer;

    #region Constructors

    public Logger(ILoggerContainer loggerContainer)
    {
      this.loggerContainer = loggerContainer ?? throw new ArgumentNullException(nameof(loggerContainer));
      AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
    }

    private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception ex)
      {
        Log(ex);
      }
    }

    #endregion Constructors

    #region Properties
  
    public List<string> Logs { get; } = new List<string>();

    public bool LogSuccess = false;

    #endregion Properties

    #region Log

    public void Log(MessageType type, object message, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string methodName = "")
    {
      try
      {
        var className = Path.GetFileNameWithoutExtension(callerFilePath);

        var log = $"[{type}|{DateTime.Now.ToString("hh:mm:ss")}]\t{className}.{methodName}()\t{message}";

        if (type == MessageType.Success && !LogSuccess)
          return;

        loggerContainer.Log(type, log);

        Logs.Add(log);
      }
      catch (Exception ex)
      {
        loggerContainer.Log(MessageType.Error, ex.Message);

        Logs.Add(ex.Message);
      }
    }

    public void Log(Exception ex)
    {
      Log(MessageType.Error, ex.ToString());
    }

    #endregion 
  }
}