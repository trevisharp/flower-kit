namespace FlowerKit.Core.Logging;

/// <summary>
/// Represents a level of log information.
/// </summary>
public enum LogLevel : byte
{
    Any = 0,
    Debug = 8,
    Info = 16,
    Warning = 32,
    Error = 64,
    Fatal = 128
}