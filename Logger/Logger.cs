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

  public class Logger
  {
    #region Fields

    private static Logger instance;

    private static ILoggerContainer LoggerContainer;

    #endregion Fields

    #region Constructors

    public Logger()
    {
      AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
    }

    private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
    }

    #endregion Constructors

    #region Properties

    public static Logger Instance
    {
      get
      {
        if (instance == null)
        {
          IKernel kernel = new StandardKernel();
          kernel.Bind<ILoggerContainer>().To<ConsoleLogger>();
          LoggerContainer = kernel.Get<ILoggerContainer>();

          instance = new Logger();
        }

        return instance;
      }
    }

    public List<string> Logs { get; } = new List<string>();

    #endregion Properties

    #region Methods

    public void Log(MessageType type, object message, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string methodName = "")
    {
      try
      {
        var className = Path.GetFileNameWithoutExtension(callerFilePath);
        var log = $"[{type}|{DateTime.Now.ToString("hh:mm:ss")}]\t{className}.{methodName}()\t{message}";
        LoggerContainer.Log(type, log);
      }
      catch (Exception ex)
      {
        LoggerContainer.Log(MessageType.Error, ex.Message);
      }
    }

    public void LogException(Exception ex, [CallerFilePath]string callerFilePath = null, [CallerMemberName]string methodName = "")
    {
      if (ex.InnerException != null)
      {
        if (ex.InnerException.InnerException != null)
        {
          Log(MessageType.Error, $"{ex.InnerException.InnerException.Message}",callerFilePath,methodName);
        }
        else
          Log(MessageType.Error, $"{ex.InnerException.Message}", callerFilePath, methodName);
      }
      else
      {
        Log(MessageType.Error, $"{ex.Message}", callerFilePath, methodName);
      }
    }

    private static string getBetween(string strSource, string strStart, string strEnd)
    {
      int Start, End;
      if (strSource.Contains(strStart) && strSource.Contains(strEnd))
      {
        Start = strSource.IndexOf(strStart, 0) + strStart.Length;
        End = strSource.IndexOf(strEnd, Start);
        return strSource.Substring(Start, End - Start);
      }
      else
      {
        return "";
      }
    }

    #endregion Methods
  }
}