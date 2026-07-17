using System;

namespace FlowerKit;

public static class LogExtension
{
    public static Log.LogConfig AddAppName(this Log.LogConfig config, string appName)
    {
        config.Formaters.Add(m => "[$appName] " + m);
        config.Variables.Add("appName", appName);
        return config;
    }

    public static Log.LogConfig AddTimeStamp(this Log.LogConfig config)
    {
        config.Formaters.Add(m => "[$timestamp] " + m);
        config.Variables.Add("timestamp", () => DateTime.UtcNow.ToString());
        return config;
    }

    public static Log.LogConfig AddLevel(this Log.LogConfig config)
    {
        config.Formaters.Add(m => "[$level] " + m);
        return config;
    }
}