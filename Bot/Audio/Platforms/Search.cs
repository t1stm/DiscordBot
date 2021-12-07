using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms.Discord;
using BatToshoRESTApp.Audio.Platforms.Spotify;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using DSharpPlus.Entities;
using Playlist = BatToshoRESTApp.Audio.Platforms.Spotify.Playlist;

namespace BatToshoRESTApp.Audio.Platforms
{
    public class Search
    {
        public async Task<List<IPlayableItem>> Get(string searchTerm, ulong length = 0)
        {
            if (searchTerm.Contains("open.spotify.com/"))
            {
                if (searchTerm.Contains("/playlist/"))
                {
                    var pl = await Playlist.Get(searchTerm.Split("playlist/").Last().Split("?")[0]);
                    return new List<IPlayableItem>(pl);
                }

                if (searchTerm.Contains("/track"))
                {
                    var tr = await Track.Get(searchTerm.Split("track/").Last().Split("?")[0]);
                    return new List<IPlayableItem> {tr};
                }
            }

            if (searchTerm.Contains("youtube.com/"))
            {
                if (searchTerm.Contains("playlist?list="))
                {
                    var pl = await new Youtube.Playlist().Get(searchTerm);
                    return new List<IPlayableItem>(pl);
                }

                if (searchTerm.Contains("watch?v="))
                    return new List<IPlayableItem>
                    {
                        await new Video().SearchById(searchTerm.Split("watch?v=").Last().Split("&")[0])
                    };
                if (searchTerm.Contains("shorts/"))
                    return new List<IPlayableItem>
                    {
                        await new Video().SearchById(searchTerm.Split("shorts/").Last().Split("&")[0])
                    };
            }

            if (searchTerm.Contains("youtu.be/"))
                return new List<IPlayableItem>
                {
                    await new Video().SearchById(searchTerm.Split("youtu.be/").Last().Split("&")[0])
                };
            if (searchTerm.StartsWith("https://www.vbox7.com/"))
                return new List<IPlayableItem>
                {
                    await new Vbox7.Video().GetVideoByUri(searchTerm.Split(".com")[1])
                };
            if (searchTerm.StartsWith("http") || searchTerm.StartsWith("https"))
                return new List<IPlayableItem>
                {
                    new OnlineFile
                    {
                        Url = searchTerm
                    }
                };

            if (searchTerm.StartsWith("file://"))
                return new List<IPlayableItem>
                {
                    new SystemFile
                    {
                        Location = searchTerm[7..],
                        Title = searchTerm,
                        Author = null,
                        Length = 0
                    }
                };
            if (searchTerm.StartsWith("vb7:"))
                return new List<IPlayableItem>
                {
                    await new Vbox7.Video().Search(searchTerm[4..])
                };
            return new List<IPlayableItem>
            {
                await new Video().Search(searchTerm, length: length)
            };
        }

        public async Task<List<IPlayableItem>> Get(string searchTerm, List<DiscordAttachment> attachments)
        {
            var list = new List<IPlayableItem>();
            list.AddRange(await Attachments.GetAttachments(attachments));
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3) return list;
            list.AddRange(await Get(searchTerm));
            return list;
        }

        public async Task<List<YoutubeVideoInformation>> Get(SpotifyTrack track)
        {
            return new()
            {
                await new Video().Search(track)
            };
        }
    }
}