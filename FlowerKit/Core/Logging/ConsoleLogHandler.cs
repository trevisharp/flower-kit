using System;

namespace FlowerKit.Core.Logging;

public class ConsoleLogHandler : LogHandler
{
    public override void Receive(LogMessage message)
    {
        Console.ForegroundColor = message.Level switch
        {
            LogLevel.Debug   => ConsoleColor.Gray,
            LogLevel.Info    => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.DarkYellow,
            LogLevel.Error   => ConsoleColor.Red,
            LogLevel.Fatal   => ConsoleColor.DarkRed,
            _                => ConsoleColor.Gray  
        };
        Console.WriteLine(message.Text);
        Console.ResetColor();
    }
}