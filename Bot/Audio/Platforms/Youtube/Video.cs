using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Methods;
using DiscordBot.Readers;
using DiscordBot.Readers.MariaDB;
using DiscordBot.Readers.Objects;
using DiscordBot.Tools;
using YoutubeExplode;
using YoutubeSearchApi.Net.Models.Youtube;
using YoutubeSearchApi.Net.Services;

namespace DiscordBot.Audio.Platforms.Youtube
{
    public static class Video
    {
        public static async Task<PlayableItem> Search(string term, bool urgent = false,
            ulong length = 0, SpotifyTrack track = null)
        {
            PlayableItem info;
            var cachedSearchResult = await GetIdFromCachedTerms(term);
            if (cachedSearchResult != null && !string.IsNullOrEmpty(cachedSearchResult.SearchTerm) &&
                !string.IsNullOrEmpty(cachedSearchResult.VideoId))
            {
                info = await GetCachedVideoFromId(cachedSearchResult.VideoId);
                if (info is not null) return info;
                return await SearchById(cachedSearchResult.VideoId);
            }

            var client = new YoutubeSearchClient(HttpClient.WithCookies());
            var response = await client.SearchAsync(term);
            var res = response.Results.ToList();
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
                    {
                        await Debug.WriteAsync($"Video Results: \"{yt.Title} - {yt.Author} - {yt.Duration}\"");
                    }
                }
                else
                {
                    res = res.OrderBy(r =>
                            Math.Abs(StringToTimeSpan.Generate(r.Duration).TotalMilliseconds - length))
                        .ToList();
                }
            }
            
            var result = res.First();
            if (result == null) return null;
            await Debug.WriteAsync(
                $"Result Milliseconds are: {StringToTimeSpan.Generate(result.Duration).TotalMilliseconds}");
            
            var alt = YoutubeOverride.FromId(result.Id);
            if (alt is not null)
            {
                return alt;
            }
            
            info = new YoutubeVideoInformation
            {
                Title = result.Title,
                Author = result.Author,
                Length = (ulong) StringToTimeSpan.Generate(result.Duration).TotalMilliseconds,
                YoutubeId = result.Id,
                SearchTerm = term,
                ThumbnailUrl = result.ThumbnailUrl
            };
            var task = new Task(async () =>
            {
                try
                {
                    //await new SearchJsonReader().AddVideo(term, result.Id);
                    //JsonWriteQueue.Add(term, result.Id);
                    await SearchValues.Add(term, result.Id);
                    await ExistingVideoInfoGetter.Add((YoutubeVideoInformation) info);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(
                        $"Adding Information to Local Library in Youtube/Video.cs/Search failed: {e}");
                }
            });
            task.Start();
            if (urgent) await info.GetAudioData();
            return info;
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
                    var source = searchTerm.Split(new[] {'.', '?', '!', ' ', ';', ':', ',', '(', ')'},
                        StringSplitOptions.RemoveEmptyEntries);
                    var times = from word in source
                        where string.Equals(word, term, StringComparison.CurrentCultureIgnoreCase)
                        select word;
                    if (times.Count() == 1) continue;
                    var things = li.Where(r => r.Title.ToLower().Contains(term.ToLower())).ToList();
                    foreach (var th in things)
                    {
                        li.Remove(th);
                        Debug.Write($"Removed result: {th.Title}", false, Debug.DebugColor.Warning);
                    }
                }
            }

            list = li;
        }

        public static async Task<List<YoutubeVideoInformation>> SearchAllResults(string term, ulong length = 0)
        {
            var client = new YoutubeSearchClient(HttpClient.WithCookies());
            var response = await client.SearchAsync(term);
            var res = response.Results.ToList();
            if (length != 0)
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(r.Duration).TotalMilliseconds - length))
                    .ToList();
            return (from YoutubeVideo video in res
                select new YoutubeVideoInformation
                {
                    Title = video.Title, Author = video.Author,
                    Length = (ulong) StringToTimeSpan.Generate(video.Duration).TotalMilliseconds, YoutubeId = video.Id,
                    ThumbnailUrl = video.ThumbnailUrl
                }).ToList();
        }

        public static async Task<PlayableItem> SearchById(string id, bool urgent = false)
        {
            try
            {
                var alt = YoutubeOverride.FromId(id);
                if (alt is not null)
                {
                    return alt;
                }
                var info = await GetCachedVideoFromId(id);
                if (info is not null && !urgent) return info;
                var client = new YoutubeClient(HttpClient.WithCookies());
                var video = await client.Videos.GetAsync(id);
                if (video is not {Duration: { }}) return null;
                var vid = new YoutubeVideoInformation
                {
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    Length = (ulong) video.Duration?.TotalMilliseconds,
                    YoutubeId = id,
                    ThumbnailUrl = video.Thumbnails[0].Url
                };
                if (urgent) await vid.GetAudioData();
                var task = new Task(async () =>
                {
                    try
                    {
                        await ExistingVideoInfoGetter.Add(vid);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Adding information in Youtube/Video.cs/SearchById {e}", true,
                            Debug.DebugColor.Urgent);
                    }
                });
                task.Start();
                return vid;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"SearchById threw exception: \"{e}\"");
                return null;
            }
        }

        private static async Task<PreviousSearchResult> GetIdFromCachedTerms(string term)
        {
            //return await new SearchJsonReader().GetVideo(term);
            return await SearchValues.ReadSearchResult(term);
        }

        private static async Task<PlayableItem> GetCachedVideoFromId(string id)
        {
            var alt = YoutubeOverride.FromId(id);
            if (alt is not null)
            {
                return alt;
            }
            return await ExistingVideoInfoGetter.Read(id);
        }

        public static async Task<PlayableItem> Search(SpotifyTrack track, bool urgent = false)
        {
            await Debug.WriteAsync($"Spotify Track: {track.GetName()}, Length: {track.GetLength()}");
            var result =
                await Search(
                    $"{track.Title} - {track.Author} {track.Explicit switch {true => "Explicit Version ", false => ""}}- Topic",
                    urgent, track.Length, track);
            result.Requester = track.GetRequester();
            if (urgent) await result.GetAudioData();
            return result;
        }
    }
}