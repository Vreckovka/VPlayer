using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class ConsoleLogger : ILoggerContainer
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
