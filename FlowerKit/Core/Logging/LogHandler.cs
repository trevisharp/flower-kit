namespace FlowerKit.Core.Logging;

/// <summary>
/// A base class to all log implementations.
/// </summary>
public abstract class LogHandler
{
    public LogLevel MinLevel { get; set; } = LogLevel.Debug;

    public abstract void Receive(LogMessage message);
}