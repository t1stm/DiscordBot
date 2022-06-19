using System;

namespace DiscordBot
{
    public static class ObjectExtensions
    {
        public static string GetRandom(this string[] array)
        {
            return array[new Random().Next(0, array.Length)];
        }
    }
}