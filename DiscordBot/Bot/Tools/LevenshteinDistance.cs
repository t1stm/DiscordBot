#nullable enable
using System;

namespace DiscordBot.Tools
{
    //Source StackOverflow: https://stackoverflow.com/questions/6944056/c-sharp-compare-string-similarity
    public static class LevenshteinDistance
    {
        public static int ComputeLean(string? s, string? t)
        {
            return ComputeStrict(RemoveFormatting(s), RemoveFormatting(t)); // I know. No need to kill me over it.
        }

        private static string? RemoveFormatting(string? str)
        {
            return str?
                .FastRemove('.')
                .FastRemove('@')
                .FastRemove('\\')
                .FastRemove('\'')
                .FastRemove('\"')
                .ToLower();
        }

        public static string FastRemove(this string source, char remove)
        {
            return string.Join(string.Empty, source.Split(remove));
        }

        public static int ComputeStrict(string? s, string? t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;

            if (string.IsNullOrEmpty(t)) return s.Length;

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (var j = 1; j <= m; d[0, j] = j++)
            {
            }

            for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
            {
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                var min1 = d[i - 1, j] + 1;
                var min2 = d[i, j - 1] + 1;
                var min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }

            return d[n, m];
        }
    }
}