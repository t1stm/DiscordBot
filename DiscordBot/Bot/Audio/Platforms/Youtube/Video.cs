#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;
using DiscordBot.Readers;
using DiscordBot.Tools;
using YoutubeExplode;
using YoutubeSearchApi.Net.Models.Youtube;
using YoutubeSearchApi.Net.Services;
using Result;
using Result.Objects;

namespace DiscordBot.Audio.Platforms.Youtube;

public static class Video
{
    public static async Task<Result<PlayableItem, Error>> Search(string term, bool urgent = false,
        ulong length = 0, SpotifyTrack? track = null)
    {
        var cachedSearchResult = GetIdFromCachedTerms(term);
        if (!string.IsNullOrEmpty(cachedSearchResult?.SearchTerm) &&
            !string.IsNullOrEmpty(cachedSearchResult.VideoId))
        {
            var cachedVideo = GetCachedVideoFromId(cachedSearchResult.VideoId);
            if (cachedVideo == Status.OK) return cachedVideo;
            var id = await SearchById(cachedSearchResult.VideoId);
            if (id == Status.OK) return id;
        }

        var client = new YoutubeSearchClient(HttpClient.WithCookies());
        var response = await client.SearchAsync(term);
        var res = response.Results.ToList();
        var copy = res.ToList();
        new Task(() =>
        {
            try
            {
                foreach (var video in copy)
                {
                    var data = new VideoInformationModel
                    {
                        VideoId = video.Id
                    };
                    var read = Databases.VideoDatabase.Read(data);
                    if (read != null) continue;
                    data.Title = video.Title;
                    data.Author = video.Author;
                    data.Length = (ulong)StringToTimeSpan.Generate(video.Duration).TotalMilliseconds;
                    data.ThumbnailUrl = video.ThumbnailUrl;

                    Databases.VideoDatabase.Add(data);
                }
            }
            catch (Exception e)
            {
                Debug.Write($"Saving all results failed. \"{e}\"");
            }
        }).Start();
        try
        {
            RemoveTheFucking18dAudio(ref res,
                term); // I hate these videos, they add nothing and in my opinion are not even worth existing on YouTube,
            // but who am I to tell people what to upload and what not. In fact let's purge these videos from even existing as a result on my bot (if not searched for).
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Removing shitty videos failed: {e}");
        }

        try
        {
            RemoveAll(ref res, term, "remix", "karaoke", "instru", "clean",
                "bass boosted", "bass", "earrape", "ear", "rape",
                "cover", "кавър", "backstage", "live", "version", "guitar",
                "extend", "maxi", "piano", "avi", "vinyl", "mix",
                "bonus", "hour", "acapella", "vocal", "matrica", "dj", "edit", "club",
                "mash", "up");
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Removing specified terms failed: {e}");
        }

        if (length != 0)
        {
            if (track is not null)
            {
                res = Sorter.SortResults(track, res);
                foreach (var yt in res.Cast<YoutubeVideo>())
                    await Debug.WriteAsync($"Video Results: \"{yt.Title} - {yt.Author} - {yt.Duration}\"");
            }
            else
            {
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(r.Duration).TotalMilliseconds - length))
                    .ToList();
            }
        }

        var result = res.First();
        if (result == null) return Result<PlayableItem, Error>.Error(new NoResultsError());
        await Debug.WriteAsync(
            $"Result Milliseconds are: {StringToTimeSpan.Generate(result.Duration).TotalMilliseconds}");

        var alt = YoutubeOverride.FromId(result.Id);
        if (alt is not null) return Result<PlayableItem, Error>.Success(alt);

        PlayableItem info = new YoutubeVideoInformation
        {
            Title = result.Title,
            Author = result.Author,
            Length = (ulong)StringToTimeSpan.Generate(result.Duration).TotalMilliseconds,
            YoutubeId = result.Id,
            SearchTerm = term,
            ThumbnailUrl = result.ThumbnailUrl
        };

        async void AddToDatabase()
        {
            try
            {
                var vid = new FuckYoutubeModel { SearchTerm = term, VideoId = result.Id };
                Databases.FuckYoutubeDatabase.Add(vid);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Adding Information to Local Library in Youtube/Video.cs/Search failed: {e}");
            }
        }

        var task = new Task(AddToDatabase);
        task.Start();
        if (urgent) await info.GetAudioData();
        return Result<PlayableItem, Error>.Success(info);
    }

    private static void RemoveTheFucking18dAudio(ref List<YoutubeVideo> list, string searchTerm)
    {
        lock (list)
        {
            var count = list.Count;
            searchTerm = searchTerm.ToLower();
            if (Regex.IsMatch(searchTerm, @"(?<!\.)\d+d")) return;
            var autisticVideo = from i in list where Regex.IsMatch(i.Title, @"(?<!\.)\d+d") select i;
            var autisticResults = autisticVideo as YoutubeVideo[] ?? autisticVideo.ToArray();
            if (autisticResults.Length == count) return;
            foreach (var autism in autisticResults)
            {
                list.Remove(autism);
                Debug.Write($"Removing autistic result: {autism.Title}", false, Debug.DebugColor.Warning);
            }
        }
    }

    private static void RemoveAll(ref List<YoutubeVideo> list, string searchTerm, params string[] terms)
    {
        var li = list;
        lock (list)
        {
            foreach (var term in terms)
            {
                if (searchTerm.ToLower().Contains(term.ToLower())) continue;
                var source = searchTerm.Split(new[] { '.', '?', '!', ' ', ';', ':', ',', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries);
                var times = from word in source
                    where LevenshteinDistance.ComputeLean(word, term) < Math.Ceiling(word.Length * 0.25)
                    select word;
                if (times.Count() == 1) continue;
                var things = li.Where(r => r.Title.ToLower().Contains(term.ToLower())).ToList();
                foreach (var thing in things)
                {
                    li.Remove(thing);
                    Debug.Write($"Removed result: {thing.Title}", false, Debug.DebugColor.Warning);
                }
            }
        }

        list = li;
    }

    public static async Task<Result<List<PlayableItem>, Error>> SearchAllResults(string term, ulong length = 0)
    {
        try
        {
            var client = new YoutubeSearchClient(HttpClient.WithCookies());
            var response = await client.SearchAsync(term);
            var res = response.Results.ToList();
            if (length != 0)
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(r.Duration).TotalMilliseconds - length))
                    .ToList();
            return Result<List<PlayableItem>, Error>.Success((from YoutubeVideo video in res
                select new YoutubeVideoInformation
                {
                    Title = video.Title, Author = video.Author,
                    Length = (ulong)StringToTimeSpan.Generate(video.Duration).TotalMilliseconds,
                    YoutubeId = video.Id,
                    ThumbnailUrl = video.ThumbnailUrl
                }).Cast<PlayableItem>().ToList());
        }
        catch (Exception)
        {
            return Result<List<PlayableItem>, Error>.Error(new UnknownError());
        }
    }

    public static async Task<Result<PlayableItem, Error>> SearchById(string id, bool urgent = false)
    {
        try
        {
            var alt = YoutubeOverride.FromId(id);
            if (alt is not null) return Result<PlayableItem, Error>.Success(alt);
            var info = GetCachedVideoFromId(id);
            if (!urgent && info == Status.OK) return info;
            var client = new YoutubeClient(HttpClient.WithCookies());
            var video = await client.Videos.GetAsync(id);
            if (video is not { Duration: { } }) return Result<PlayableItem, Error>.Error(new NoResultsError());
            var vid = new YoutubeVideoInformation
            {
                Title = video.Title,
                Author = video.Author.ChannelTitle,
                Length = (ulong)(video.Duration?.TotalMilliseconds ?? 0),
                YoutubeId = id,
                ThumbnailUrl = video.Thumbnails[0].Url
            };
            if (urgent) await vid.GetAudioData();
            var task = new Task(() =>
            {
                try
                {
                    var data = new VideoInformationModel
                    {
                        VideoId = video.Id
                    };
                    var read = Databases.VideoDatabase.Read(data);
                    if (read != null) return;
                    data.Title = vid.Title;
                    data.Author = vid.Author;
                    data.Length = vid.Length;
                    data.ThumbnailUrl = vid.ThumbnailUrl;

                    Databases.VideoDatabase.Add(data);
                }
                catch (Exception e)
                {
                    Debug.Write($"Adding information in Youtube/Video.cs/SearchById failed: \"{e}\"", true,
                        Debug.DebugColor.Urgent);
                }
            });
            task.Start();
            return Result<PlayableItem, Error>.Success(vid);
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"SearchById threw exception: \"{e}\"");
            return Result<PlayableItem, Error>.Error(new UnknownError());
        }
    }

    private static FuckYoutubeModel? GetIdFromCachedTerms(string term)
    {
        //return await new SearchJsonReader().GetVideo(term);
        var vid = new FuckYoutubeModel
        {
            SearchTerm = term.ToLower()
        };
        return Databases.FuckYoutubeDatabase.Read(vid);
    }

    private static Result<PlayableItem, Error> GetCachedVideoFromId(string id)
    {
        var alt = YoutubeOverride.FromId(id);
        if (alt is not null) return Result<PlayableItem, Error>.Success(alt);
        var search = new VideoInformationModel
        {
            VideoId = id
        };
        var result = Databases.VideoDatabase.Read(search)?.Convert();
        return result != null
            ? Result<PlayableItem, Error>.Success(result)
            : Result<PlayableItem, Error>.Error(new CacheNotFoundError());
    }

    public static async Task<Result<PlayableItem, Error>> Search(SpotifyTrack track, bool urgent = false)
    {
        await Debug.WriteAsync($"Spotify Track: {track.GetName()}, Length: {track.GetLength()}");
        var result =
            await Search(
                $"{track.Title} - {track.Author} {track.Explicit switch { true => "Explicit Version ", false => "" }}- Topic",
                urgent, track.Length, track);
        if (result != Status.OK) return result;

        var response = result.GetOK();
        response.Requester = track.GetRequester();
        if (urgent) await response.GetAudioData();
        return result;
    }
}