using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Audio.Objects;
using DiscordBot.Tools;
using YoutubeSearchApi.Net.Models.Youtube;

namespace DiscordBot.Audio.Platforms.Youtube
{
    public static class Sorter
    {
        public static List<YoutubeVideo> SortResults(SpotifyTrack track, IEnumerable<YoutubeVideo> pls)
        {
            var res = pls.ToList();

            var exact = res.AsParallel().Any(r =>
                LevenshteinDistance.Compute(track.Author.ToLower(), r.Author.ToLower()) < 2 &&
                LevenshteinDistance.Compute(track.Title.ToLower(), r.Title.ToLower()) < 2);
            
            if (exact)
            {
                return res.AsParallel().OrderByDescending(r =>
                    LevenshteinDistance.Compute(track.Author.ToLower(), r.Author.ToLower()) < 4 &&
                    LevenshteinDistance.Compute(track.Title.ToLower(), r.Title.ToLower()) < 4).ToList();
            }
            
            var oneArtist = res.AsParallel().Any(r =>
                LevenshteinDistance.Compute(track.Author.ToLower().Split(',')[0].Trim(), r.Author.ToLower()) < 4 &&
                LevenshteinDistance.Compute(track.Title.ToLower(), r.Title.ToLower()) < 4);

            if (oneArtist)
            {
                return res.AsParallel().OrderByDescending(r =>
                    LevenshteinDistance.Compute(track.Author.ToLower().Split(',')[0].Trim(), r.Author.ToLower()) < 4 &&
                    LevenshteinDistance.Compute(track.Title.ToLower(), r.Title.ToLower()) < 4).ToList();
            }
            
            return res.AsParallel()
                .OrderByDescending(r =>
                    LevenshteinDistance.Compute(track.Author.ToLower(), r.Author.ToLower()) < 4 ||
                    r.Author is "Chris R" or "Craig Gagné")
                .ThenByDescending(r =>
                    LevenshteinDistance.Compute($"{track.Author.ToLower()} - topic", r.Author.ToLower()) < 4)
                .ThenByDescending(r =>
                    LevenshteinDistance.Compute($"{track.Author.ToLower()} official", r.Author.ToLower()) < 4)
                .ThenByDescending(r =>
                    LevenshteinDistance.Compute($"official {track.Author.ToLower()}", r.Author.ToLower()) < 4)
                .ThenByDescending(r => Math.Abs((int) (track.Length - StringToTimeSpan.Generate(r.Duration).TotalMilliseconds)) < 3000)
                .ThenByDescending(r => r.Title.ToLower().Contains("lyric"))
                .ThenByDescending(r => r.Title.ToLower().Contains("official audio"))
                .ThenBy(r => LevenshteinDistance.Compute(r.Title, track.Title))
                .ThenBy(r => LevenshteinDistance.Compute(r.Title, $"{track.Author} - {track.Title}"))
                .ThenBy(r => LevenshteinDistance.Compute(r.Title, $"{track.Title} - {track.Author}"))
                .ThenByDescending(r =>
                    LevenshteinDistance.Compute(
                        r.Author.ToLower().Replace("- topic", "").Replace(" ", ""),
                        track.Author.ToLower().Replace(" ", "")) < 3)
                .ThenByDescending(r => Math.Abs((int) (track.Length - StringToTimeSpan.Generate(r.Duration).TotalMilliseconds)) < 3000)
                .ToList();
        }
    }
}