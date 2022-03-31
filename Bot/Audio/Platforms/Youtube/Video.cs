using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using BatToshoRESTApp.Readers.MariaDB;
using BatToshoRESTApp.Tools;
using YoutubeExplode;
using YoutubeSearchApi.Net;
using YoutubeSearchApi.Net.Backends;
using YoutubeSearchApi.Net.Objects;

namespace BatToshoRESTApp.Audio.Platforms.Youtube
{
    public class Video
    {
        public async Task<YoutubeVideoInformation> Search(string term, bool urgent = false,
            ulong length = 0, SpotifyTrack track = null)
        {
            YoutubeVideoInformation info;
            var jsonId = await GetIdFromJson(term);
            if (jsonId != null && !string.IsNullOrEmpty(jsonId.SearchTerm) && !string.IsNullOrEmpty(jsonId.VideoId))
            {
                info = await GetCachedVideoFromId(jsonId.VideoId);
                if (info is not null) return info;
                return await SearchById(jsonId.VideoId);
            }

            var client = new DefaultSearchClient(new YoutubeSearchBackend());
            var response = await client.SearchAsync(HttpClient.WithCookies(), term, 25);
            var res = response.Results.ToList();
            RemoveTheFucking18dAudio(ref res, term); // I hate these videos, they add nothing and in my opinion are not even worth existing on YouTube,
            // but who am I to tell people what to upload and what not, so let's fucking purge the results from even existing.
            RemoveAll(ref res, term, "remix", "karaoke", "instru", "clean", "bass boosted", "bass", "ear rape", "earrape", "ear", "rape", 
                "cover", "кавър", "backstage", "live", "version");
            if (length != 0)
            {
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(((YoutubeVideo) r).Duration).TotalMilliseconds - length))
                    .ToList();
                if (track is {})
                {
                    /*if (!term.ToLower().Contains("8d") && !term.ToLower().Contains("karaoke") &&
                        !term.ToLower().Contains("remix") && !term.ToLower().Contains("instru") && !term.ToLower().Contains("clean"))
                        res.RemoveAll(r =>
                            r.Title.ToLower().Contains("8d") || r.Title.ToLower().Contains("karaoke") ||
                            r.Title.ToLower().Contains("remix") || r.Title.ToLower().Contains("instru") ||
                            r.Title.ToLower().Contains("clean")); */
                    if (res.Any(r => r.Title.ToLower().Contains("official audio"))) res = res.OrderBy(r => r.Title.ToLower().Contains("official audio")).ToList();
                    res = res.OrderBy(r => LevenshteinDistance.Compute(r.Title, track.Title) < 3).ToList();
                }
            }
            
            var result = (YoutubeVideo) res.First();
            if (result == null) return null;
            await Debug.WriteAsync(
                $"Result Milliseconds are: {StringToTimeSpan.Generate(result.Duration).TotalMilliseconds}");
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
                    JsonWriteQueue.Add(term, result.Id);
                    await new ExistingVideoInfoGetter().Add(info);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(
                        $"Adding Information to Local Library in Youtube/Video.cs/Search failed: {e}");
                }
            });
            task.Start();
            if (urgent) await info.Download();
            return info;
        }

        private static void RemoveTheFucking18dAudio(ref List<IResponseResult> list, string searchTerm)
        {
            lock (list)
            {
                searchTerm = searchTerm.ToLower();
                if (Regex.IsMatch(searchTerm, @"(?<!\.)\d+d")) return;
                var autisticVideo = list.Where(i => Regex.IsMatch(@"(?<!\.)\d+d", i.Title));
                foreach (var autism in autisticVideo)
                {
                    list.Remove(autism);
                    Debug.Write($"Removing autistic result: {autism.Title}", false, Debug.DebugColor.Warning);
                }
            }
        }

        private static void RemoveAll(ref List<IResponseResult> list, string searchTerm, params string[] terms)
        {
            var li = list;
            lock (list)
            {
                foreach (var term in terms)
                {
                    if (searchTerm.ToLower().Contains(term.ToLower())) continue;
                    var source = searchTerm.Split(new[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
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

        public async Task<List<YoutubeVideoInformation>> SearchAllResults(string term, bool urgent = false,
            ulong length = 0)
        {
            var client = new DefaultSearchClient(new YoutubeSearchBackend());
            var response = await client.SearchAsync(HttpClient.WithCookies(), term, 25);
            var res = response.Results.ToList();
            if (length != 0)
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(((YoutubeVideo) r).Duration).TotalMilliseconds - length))
                    .ToList();
            return (from YoutubeVideo video in res
                select new YoutubeVideoInformation
                {
                    Title = video.Title, Author = video.Author,
                    Length = (ulong) StringToTimeSpan.Generate(video.Duration).TotalMilliseconds, YoutubeId = video.Id
                }).ToList();
        }

        public async Task<YoutubeVideoInformation> SearchById(string id, bool urgent = false)
        {
            try
            {
                var info = await GetCachedVideoFromId(id);
                if (info is not null && !urgent) return info;
                var client = new YoutubeClient(HttpClient.WithCookies());
                var video = await client.Videos.GetAsync(id);
                if (video is not {Duration: { }}) return null;
                var vid = new YoutubeVideoInformation
                {
                    Title = video.Title,
                    Author = video.Author.Title,
                    Length = (ulong) video.Duration?.TotalMilliseconds,
                    YoutubeId = id,
                    ThumbnailUrl = video.Thumbnails[0].Url,
                    IsId = true
                };
                if (urgent) await vid.Download();
                var task = new Task(async () =>
                {
                    try
                    {
                        await new ExistingVideoInfoGetter().Add(vid);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Adding information in Youtube/Video.cs/SearchById {e}", true, Debug.DebugColor.Urgent);
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

        private static async Task<PreviousSearchResult> GetIdFromJson(string term)
        {
            return await new SearchJsonReader().GetVideo(term);
        }

        private static async Task<YoutubeVideoInformation> GetCachedVideoFromId(string id)
        {
            return await new ExistingVideoInfoGetter().Read(id);
        }

        public async Task<YoutubeVideoInformation> Search(SpotifyTrack track, bool urgent = false)
        {
            await Debug.WriteAsync($"Track: {track.GetName()}, Length: {track.GetLength()}");
            var result = await Search($"{track.Title} - {track.Author} {track.Explicit switch {true => "Explicit Version ", false => ""}}- Topic", urgent, track.Length);
            result.OriginTrack = track;
            result.Requester = track.GetRequester();
            if (urgent) await result.Download();
            return result;
        }
    }
}