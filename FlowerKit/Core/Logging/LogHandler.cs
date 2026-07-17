namespace FlowerKit.Core.Logging;

public abstract class LogHandler
{
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    public abstract void Receive(LogMessage message);
}