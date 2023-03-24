using System;
using System.Linq;

namespace DiscordBot.Tools;

public static class StringToTimeSpan
{
    public static TimeSpan
        Generate(string text) //This is the most beautiful peace of code I've ever created to this date.
    {
        long milliseconds = 0;
        var ms = text.Contains('.');
        text = ms ? text.Replace(".", ":") : text;
        var time = text.Split(":").Reverse().ToArray();
        for (var i = ms ? 0 : 1; i < time.Length + (ms ? 0 : 1); i++)
            try
            {
                var q = long.Parse(time[i - (ms ? 0 : 1)]);
                milliseconds += i switch
                {
                    0 => q,
                    1 => q * 1000,
                    2 => q * 60000,
                    3 => q * 3600000,
                    4 => q * 86400000,
                    5 => q * 604800000,
                    _ => throw new ArgumentOutOfRangeException(nameof(i),
                        new Exception("Millisecond steps are more than a decade."))
                };
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}