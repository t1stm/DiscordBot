#nullable enable
using System;
using System.Threading.Tasks;

namespace DiscordBot;

public static class Extensions
{
    public static T ThrowIfNull<T>(this T? obj)
    {
        return obj ?? throw new Exception("The variable is null.");
    }

    public static Task ExecuteIfNotNull(this Task? task)
    {
        return task ?? Task.CompletedTask;
    }
}