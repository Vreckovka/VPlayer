using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public class ConsoleLogger : ILoggerContainer
    {
        public Task Log(MessageType messageType, string message)
        {
          
            //… makes beep sound
            return Task.Run(() =>
            {
                switch (messageType)
                {
                    case MessageType.Inform:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(message.Replace("…", ""));
                        break;
                    case MessageType.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message.Replace("…", ""));
                        break;
                    case MessageType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(message.Replace("…", ""));
                        break;
                    case MessageType.Success:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(message.Replace("…",""));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
                }
            });

        }
    }
}
