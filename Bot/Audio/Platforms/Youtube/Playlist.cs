using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace Bat_Tosho.Audio.Platforms.Youtube
{
    public static class Playlist
    {
        public static async Task<List<VideoInformation>> Get(string path, DiscordUser user)
        {
            var client = new YoutubeClient();
            var playlist = await client.Playlists.GetVideosAsync(path);
            return playlist.Where(v => v.Duration != null).Select(video => new VideoInformation(video.Id,
                    VideoSearchTypes.NotDownloaded, PartOf.YoutubePlaylist, video.Title,
                    video.Author.Title,
                    (int) video.Duration.GetValueOrDefault().TotalMilliseconds, user, null,
                    video.Thumbnails.First().Url))
                .ToList();
        }
    }
}