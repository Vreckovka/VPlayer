using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;

namespace Logger
{
    public class Logger
    {
        
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

        public void Log(Type t, string message)
        {
            var log = $"{DateTime.Now.ToString("hh:mm:ss")}\t{t.Name}\t{message}";
            LoggerContainer.Log(log);
        }
    }
}
