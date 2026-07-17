using System.Collections.Generic;

namespace FlowerKit.Core.Logging;

/// <summary>
/// Represents a log message.
/// </summary>
public record LogMessage(
    string Text,
    LogLevel Level,
    Dictionary<string, string> Keys
);