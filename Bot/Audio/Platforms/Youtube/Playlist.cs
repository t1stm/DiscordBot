using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace BatToshoRESTApp.Audio.Platforms.Youtube
{
    public class Playlist
    {
        public async Task<List<YoutubeVideoInformation>> Get(string url)
        {
            var playlist = await new YoutubeClient().Playlists.GetVideosAsync(FixPlaylistUrl(url));
            return playlist.Where(vi => vi.Duration != null).Select(video =>
            {
                if (video.Duration?.TotalMilliseconds != null)
                    return new YoutubeVideoInformation
                    {
                        Title = video.Title,
                        Author = video.Author.Title,
                        Length = (ulong) video.Duration?.TotalMilliseconds,
                        ThumbnailUrl = video.Thumbnails[0].Url,
                        YoutubeId = video.Id
                    };
                return null;
            }).ToList();
        }

        private static string FixPlaylistUrl(string url)
        {
            return url.Split("playlist?list=").Last().Split("&")[0];
        }
    }
}