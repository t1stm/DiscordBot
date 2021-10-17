using System;
using System.Linq;

namespace Bat_Tosho.Methods
{
    public static class Return
    {
        public static TimeSpan StringToTimeSpan(string text) //This is the most beautiful peace of code I've ever created to this date.
        {
            long milliseconds = 0;
            bool ms = text.Contains(".");
            text = ms ? text.Replace(".", ":") : text;
            string[] time = text.Split(":").Reverse().ToArray();
            for (int i = ms ? 0 : 1; i < time.Length; i++)
            {
                long q = long.Parse(time[i-(ms ? 0 : -1)]);
                milliseconds += i switch
                {
                    0 => q,
                    1 => q*1000,
                    2 => q*60000,
                    3 => q*3600000,
                    4 => q*86400000,
                    5 => q*604800000,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        public static string RandomString(int length, bool includeBadSymbols = false) => new (Enumerable
            .Repeat(
                $"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{includeBadSymbols switch {true => "_-.", false => ""}}",
                length).Select(s => s[new Random(Program.Rng.Next(int.MaxValue)).Next(s.Length)]).ToArray());
    }
}