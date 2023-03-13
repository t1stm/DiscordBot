using System;

#nullable enable
namespace DiscordBot
{
    public static class Extensions
    {
        public static T ThrowIfNull<T>(this T? obj)
        {
            return obj ?? throw new Exception("The variable is null.");
        }
    }
}