using System;
using System.Linq;
using System.Collections.Generic;

namespace FlowerKit;

using Core.Logging;

public static class Log
{
    public record LogConfig(
        List<LogHandler> Handlers,
        Dictionary<string, object> Variables,
        List<Func<string, string>> Formaters
    );

    readonly static List<LogHandler> Handlers = [
        new ConsoleLogHandler { MinLevel = LogLevel.Any }
    ];
    readonly static Dictionary<string, object> globalkeys = [];
    readonly static List<Func<string, string>> globalFormaters = [];

    public static LogConfig Config => new(Handlers, globalkeys, globalFormaters);

    public static void Debug(string message, params (string, object)[] parameters)
        => SendMessage(LogLevel.Debug, message, parameters);
    
    public static void Info(string message, params (string, object)[] parameters)
        => SendMessage(LogLevel.Info, message, parameters);

    public static void Warning(string message, params (string, object)[] parameters)
        => SendMessage(LogLevel.Warning, message, parameters);
    
    public static void Error(string message, params (string, object)[] parameters)
        => SendMessage(LogLevel.Error, message, parameters);
    
    public static void Fatal(string message, params (string, object)[] parameters)
        => SendMessage(LogLevel.Fatal, message, parameters);

    static void SendMessage(LogLevel level, string message, params (string, object)[] parameters)
    {
        var logMessage = BuildLogMessage(level, message, parameters);
        foreach (var handler in Handlers)
        {
            if (level < handler.MinLevel)
                continue;
            
            handler.Receive(logMessage);
        }
    }
    
    static LogMessage BuildLogMessage(LogLevel level, string message, params (string, object)[] parameters)
    {
        foreach (var formater in globalFormaters)
            message = formater(message);

        var dict = globalkeys
            .Select(k => (k.Key, k.Value))
            .Union(parameters)
            .Append(("level", level.ToString()))
            .ToDictionary(pair => pair.Item1, pair => pair.Item2 switch
            {
                string text => text,
                Func<string> func => func(),
                Func<object> func => func().ToString() ?? "null",
                not null => pair.Item2.ToString() ?? "null",
                _ => "null"
            });
        
        message = Format(message, dict);

        return new LogMessage(message, level, dict);
    }

    static string Format(string message, Dictionary<string, string> keys)
    {
        foreach (var (key, value) in keys)
        {
            var realKey = $"${key}";
            if (!message.Contains(realKey))
                continue;

            message = message.Replace(realKey, value);
        }
        
        return message;
    }
}