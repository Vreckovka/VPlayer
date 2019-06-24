using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ninject;

namespace Logger
{
    public class Logger
    {

        public static void ThreadTest()
        {
            while (true)
            {
                Console.WriteLine("Thread test");
            }

            ;
        }
        public List<string> Logs { get; } = new List<string>();

        private static ILoggerContainer LoggerContainer;
        private static Logger instance;

        public Logger()
        {

        }
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

        public void Log(MessageType type, string message)
        {
            try
            {
                // Get call stack
                StackTrace stackTrace = new StackTrace();

                // Get calling method name

                var method = stackTrace.GetFrame(1).GetMethod();
                var cls = method.ReflectedType?.UnderlyingSystemType.Name;

                var methodName = getBetween(cls, "<", ">");
                var className = method.ReflectedType?.DeclaringType?.Name;

                //var log = $"{DateTime.Now.ToString("hh:mm:ss")}\t{t.Name}\t{message}";

                var log = $"[{type}|{DateTime.Now.ToString("hh:mm:ss")}]\t{className}.{methodName}()\t{message}";
                LoggerContainer.Log(type, log);
            }
            catch (Exception ex)
            {
                LoggerContainer.Log(MessageType.Error, ex.Message);
            }
        }

        public void LogException(Exception ex)
        {
            if (ex.InnerException != null)
            {
                if (ex.InnerException.InnerException != null)
                {
                    Log(MessageType.Error, $"{ex.InnerException.InnerException.Message}");
                }
                else
                    Log(MessageType.Error, $"{ex.InnerException.Message}");

            }
            else
            {
                Log(MessageType.Error, $"{ex.Message}");

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
    }

    public enum MessageType
    {
        Inform,
        Success,
        Error,
        Warning
    }
}
