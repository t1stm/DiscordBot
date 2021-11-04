using System;
using System.Linq;
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
            ulong length = 0)
        {
            YoutubeVideoInformation info;
            var foundInJson = false;
            var jsonId = GetIdFromJson(term);
            if (jsonId != null && !string.IsNullOrEmpty(jsonId.SearchTerm) && !string.IsNullOrEmpty(jsonId.VideoId))
            {
                foundInJson = true;
                info = await GetCachedVideoFromId(jsonId.VideoId);
                if (info is not null) return info;
            }

            var client = new DefaultSearchClient(new YoutubeSearchBackend());
            var response = await client.SearchAsync(HttpClient.WithCookies(), term, 10);
            var res = response.Results.ToList();
            if (length != 0)
                res = res.OrderBy(r =>
                        Math.Abs(StringToTimeSpan.Generate(((YoutubeVideo) r).Duration).TotalMilliseconds - length))
                    .ToList();

            await Debug.WriteAsync($"Search term is: {term}");
            await Debug.WriteAsync($"Search Milliseconds are: {length}");
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
            if (!foundInJson) new SearchJsonReader().AddVideo(term, result.Id);
            await new ExistingVideoInfoGetter().Add(info);
            var task = new Task(async () =>
            {
                if (!foundInJson) new SearchJsonReader().AddVideo(term, result.Id);
                await new ExistingVideoInfoGetter().Add(info);
            });
            task.Start();
            if (urgent) await info.Download();
            return info;
        }

        public async Task<YoutubeVideoInformation> SearchById(string id, bool urgent = false)
        {
            var info = await GetCachedVideoFromId(id);
            if (info is not null && !urgent) return info;
            var client = new YoutubeClient();
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
            return vid;
        }

        private PreviousSearchResult GetIdFromJson(string term)
        {
            return new SearchJsonReader().GetVideo(term);
        }

        private async Task<YoutubeVideoInformation> GetCachedVideoFromId(string id)
        {
            return await new ExistingVideoInfoGetter().Read(id);
        }

        public async Task<YoutubeVideoInformation> Search(SpotifyTrack track, bool urgent = false)
        {
            await Debug.WriteAsync($"Track: {track.GetName()}, Length: {track.GetLength()}");
            var result = await Search($"{track.Title} - {track.Author} - Topic", urgent, track.Length);
            result.OriginTrack = track;
            if (urgent) await result.Download();
            return result;
        }
    }
}