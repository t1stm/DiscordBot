using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Audio.Objects;

namespace TestingApp.Search_Algorithm_Tests
{
    public static class RankOverrides
    {
        public static void Rank()
        {
            string? read;
            var copy = YoutubeOverride.Overrides.ToList();
            while ((read = Console.ReadLine()) is not "exit" or null)
            {
                var term = read;
                var sorted = ReturnBestMatches(copy, term);
                for (var i = 0; i < (sorted.Count > 10 ? 10 : sorted.Count); i++)
                {
                    var el = sorted[i];
                    Console.WriteLine($"({i}) - {el.GetName()}");
                }
            }
            
        }

        private static List<YoutubeOverride> ReturnBestMatches(IReadOnlyCollection<YoutubeOverride> database, string? term)
        {
            var scored = new List<YoutubeOverride>();
            if (string.IsNullOrEmpty(term)) return scored;
            //TODO: Implement score by title and then author.
            var termSplit = term.Split(' ');
            var currentIndex = 0;
            
            var authors = database.AsParallel().Select(r => r.GetAuthor()).OrderBy(r => r.Length).ToList();
            var titles = database.AsParallel().Select(r => r.GetTitle()).OrderBy(r => r.Length).ToList();
            var score = new Score();

            return scored;
        }
    }

    public class Score
    {
        public double Title, Author;
    }
}