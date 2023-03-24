using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Audio.Objects;
using DiscordBot.Tools;
using YoutubeSearchApi.Net.Models.Youtube;

namespace DiscordBot.Audio.Platforms.Youtube;

public static class Sorter
{
    public static List<YoutubeVideo> SortResults(SpotifyTrack track, IEnumerable<YoutubeVideo> pls)
    {
        var res = pls.ToList();

        var exact = res.AsParallel().Any(r =>
            LevenshteinDistance.ComputeLean(track.Author, r.Author) < 2 &&
            LevenshteinDistance.ComputeLean(track.Title, r.Title) < 2);

        if (exact)
            return res.AsParallel().OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author, r.Author) < 4 &&
                LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4).ToList();

        var oneArtist = res.AsParallel().Any(r =>
            LevenshteinDistance.ComputeLean(track.Author.Split(',')[0].Trim(), r.Author) < 4 &&
            LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4);

        if (oneArtist)
            return res.AsParallel().OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author.Split(',')[0].Trim(), r.Author) < 4 &&
                LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4).ToList();

        return res.AsParallel()
            .OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author, r.Author) < 4 ||
                r.Author is "Chris R" or "Craig Gagné")
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"{track.Author} - topic", r.Author) < 4)
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"{track.Author} official", r.Author) < 4)
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"official {track.Author}", r.Author) < 4)
            .ThenByDescending(r =>
                Math.Abs((int)(track.Length - StringToTimeSpan.Generate(r.Duration).TotalMilliseconds)) < 3000)
            .ThenByDescending(r => r.Title.Contains("lyric"))
            .ThenByDescending(r => r.Title.Contains("official audio"))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, track.Title))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, $"{track.Author} - {track.Title}"))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, $"{track.Title} - {track.Author}"))
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean(
                    r.Author.Replace("- topic", "").Replace(" ", ""),
                    track.Author.Replace(" ", "")) < 3)
            .ThenByDescending(r =>
                Math.Abs((int)(track.Length - StringToTimeSpan.Generate(r.Duration).TotalMilliseconds)) < 3000)
            .ToList();
    }
}