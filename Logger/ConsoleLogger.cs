using System;
using System.Threading.Tasks;

namespace Logger
{
  public class ConsoleLogger : ILoggerContainer
  {
    #region Methods

    public Task Log(MessageType messageType, string message)
    {
      //… makes beep sound
      return Task.Run(() =>
      {
        Console.ForegroundColor = ConsoleColor.White;

        switch (messageType)
        {
          case MessageType.Inform:
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
            Console.WriteLine(message.Replace("…", ""));
            break;

          default:
            throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
        }

        Console.ForegroundColor = ConsoleColor.White;
      });
    }

    #endregion Methods
  }
}