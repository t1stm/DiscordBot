using System;
using System.Linq;
using DiscordBot.Abstract;
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
                var sorted = copy.AsParallel().OrderByDescending(r => CalculateScore(r, term ?? "")).ToList();
                for (var i = 0; i < (sorted.Count > 10 ? 10 : sorted.Count); i++)
                {
                    var el = sorted[i];
                    Console.WriteLine($"({i}) - {el.GetName()}");
                }
            }
            
        }

        private static double CalculateScore(PlayableItem ov, string term)
        {
            var score = 0d;
            if (string.IsNullOrEmpty(term)) return score;
            //TODO: Implement score by title and then author.
            var authorIndex = -1;
            
            var title = ov.GetTitle();
            var author = ov.GetAuthor();
            
            

            return score;
        }
    }

    public class Score
    {
        public double TitleScore, AuthorScore;
    }
}