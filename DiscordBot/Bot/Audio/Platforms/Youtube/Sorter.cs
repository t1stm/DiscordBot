using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Audio.Objects;
using DiscordBot.Tools;
using YoutubeExplode.Search;

namespace DiscordBot.Audio.Platforms.Youtube;

public static class Sorter
{
    public static List<VideoSearchResult> SortResults(SpotifyTrack track, IEnumerable<VideoSearchResult> pls)
    {
        var res = pls.ToList();

        var exact = res.AsParallel().Any(r =>
            LevenshteinDistance.ComputeLean(track.Author, r.Author.ChannelTitle) < 2 &&
            LevenshteinDistance.ComputeLean(track.Title, r.Title) < 2);

        if (exact)
            return res.AsParallel().OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author, r.Author.ChannelTitle) < 4 &&
                LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4).ToList();

        var oneArtist = res.AsParallel().Any(r =>
            LevenshteinDistance.ComputeLean(track.Author.Split(',')[0].Trim(), r.Author.ChannelTitle) < 4 &&
            LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4);

        if (oneArtist)
            return res.AsParallel().OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author.Split(',')[0].Trim(), r.Author.ChannelTitle) < 4 &&
                LevenshteinDistance.ComputeLean(track.Title, r.Title) < 4).ToList();

        return res.AsParallel()
            .OrderByDescending(r =>
                LevenshteinDistance.ComputeLean(track.Author, r.Author.ChannelTitle) < 4 ||
                r.Author.ChannelTitle is "Chris R" or "Craig Gagné")
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"{track.Author} - topic", r.Author.ChannelTitle) < 4)
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"{track.Author} official", r.Author.ChannelTitle) < 4)
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean($"official {track.Author}", r.Author.ChannelTitle) < 4)
            .ThenByDescending(r =>
                Math.Abs((int)(track.Length - (r.Duration?.TotalMilliseconds ?? 0))) < 3000)
            .ThenByDescending(r => r.Title.Contains("lyric"))
            .ThenByDescending(r => r.Title.Contains("official audio"))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, track.Title))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, $"{track.Author} - {track.Title}"))
            .ThenBy(r => LevenshteinDistance.ComputeStrict(r.Title, $"{track.Title} - {track.Author}"))
            .ThenByDescending(r =>
                LevenshteinDistance.ComputeLean(
                    r.Author.ChannelTitle.Replace("- topic", "").Replace(" ", ""),
                    track.Author.Replace(" ", "")) < 3)
            .ThenByDescending(r =>
                Math.Abs((int)(track.Length - (r.Duration?.TotalMilliseconds ?? 0))) < 3000)
            .ToList();
    }
}