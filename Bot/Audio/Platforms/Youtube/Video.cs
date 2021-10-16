using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using Bat_Tosho.Methods;
using DSharpPlus.Entities;
using YoutubeExplode;
using YoutubeSearchApi.Net;
using YoutubeSearchApi.Net.Backends;
using YoutubeSearchApi.Net.Objects;

namespace Bat_Tosho.Audio.Platforms.Youtube
{
    public static class Video
    {
        public static async Task<List<VideoInformation>> Get(string path, VideoSearchTypes type, PartOf partOf,
            DiscordUser user)
        {
            switch (type)
            {
                case VideoSearchTypes.SearchTerm:
                    var youtube = new DefaultSearchClient(new YoutubeSearchBackend());
                    while (path[0] == '-')
                        path = path[1..];
                    await Debug.Write($"path is: {path}", false);
                    var results = await youtube.SearchAsync(HttpClient.WithCookies(), path, 5);
                    var result = (YoutubeVideo) results.Results.First();

                    if (result == null) return null;

                    return new List<VideoInformation>
                    {
                        new(result.Id, VideoSearchTypes.NotDownloaded, partOf, result.Title, result.Author,
                            (int) new Return().StringToTimeSpan(result.Duration).TotalMilliseconds, user, null, result
                                .ThumbnailUrl)
                    };
                case VideoSearchTypes.YoutubeVideoId:
                    path = path.Split("?v=").Last().Split("&").First();
                    var client = new YoutubeClient();
                    var video = await client.Videos.GetAsync(path);
                    return new List<VideoInformation>
                    {
                        new(video.Id, VideoSearchTypes.NotDownloaded, partOf, video.Title, video.Author.Title,
                            video.Duration.HasValue switch
                            {
                                false => -1, true => (int) video.Duration.Value.TotalMilliseconds
                            }, user, null, video.Thumbnails.First().Url)
                    };
            }

            return null;
        }
    }
}