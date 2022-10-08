using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Objects;

namespace DiscordBot
{
    public static class ObjectExtensions
    {
        private static readonly Random Random = new();

        public static string CodeBlocked(this string str)
        {
            return $"```{str}```";
        }

        public static string GetRandom(this string[] array)
        {
            return array[Random.Next(0, array.Length)];
        }

        // Man I am so lazy, you won't fucking believe it. These next few methods are prime examples of that.
        public static bool ContainsValue(this List<User> users, string token)
        {
            return users.AsReadOnly().AsParallel().Any(r => r.Token == token);
        }

        public static bool ContainsKey(this List<User> users, ulong id)
        {
            return users.AsReadOnly().AsParallel().Any(r => r.Id == id);
        }

        public static string GetValue(this List<User> users, ulong key)
        {
            return users.AsReadOnly().AsParallel().FirstOrDefault(r => r.Id == key)?.Token;
        }

        public static string GetKey(this List<User> users, string value)
        {
            return users.AsReadOnly().AsParallel().FirstOrDefault(r => r.Token == value)?.Token;
        }

        public static User Get(this List<User> users, ulong key)
        {
            return users.AsReadOnly().AsParallel().FirstOrDefault(r => r.Id == key);
        }

        public static User Get(this List<User> users, string value)
        {
            return users.AsReadOnly().AsParallel().FirstOrDefault(r => r.Token == value);
        }

        public static IEnumerable<ulong> Keys(this List<User> users)
        {
            return users.AsReadOnly().AsParallel().Select(r => r.Id);
        }

        public static IEnumerable<string> Values(this List<User> users)
        {
            return users.AsReadOnly().AsParallel().Select(r => r.Token);
        }
    }
}