#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace DiscordBot.Audio.Platforms.Youtube;

public static class Playlist
{
    public static async Task<Result<List<PlayableItem>, Error>> Get(string url)
    {
        try
        {
            var playlist = await new YoutubeClient().Playlists.GetVideosAsync(FixPlaylistUrl(url));

            var list = (from video in playlist
                    where video.Duration?.TotalMilliseconds != null
                    select new YoutubeVideoInformation
                    {
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        Length = (ulong)(video.Duration?.TotalMilliseconds ?? 0),
                        ThumbnailUrl = video.Thumbnails[0].Url,
                        YoutubeId = video.Id
                    }).Cast<PlayableItem>()
                .ToList();

            return Result<List<PlayableItem>, Error>.Success(list);
        }
        catch (Exception)
        {
            return Result<List<PlayableItem>, Error>.Error(new UnknownError());
        }
    }

    private static string FixPlaylistUrl(string url)
    {
        return url.Split("playlist?list=").Last().Split("&")[0];
    }
}