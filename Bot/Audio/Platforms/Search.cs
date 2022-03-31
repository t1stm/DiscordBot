using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms.Discord;
using BatToshoRESTApp.Audio.Platforms.Local;
using BatToshoRESTApp.Audio.Platforms.Spotify;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Methods;
using DSharpPlus.Entities;
using Playlist = BatToshoRESTApp.Audio.Platforms.Spotify.Playlist;

namespace BatToshoRESTApp.Audio.Platforms
{
    public class Search
    {
        public async Task<List<IPlayableItem>> Get(string searchTerm, ulong length = 0, bool returnAllResults = false)
        {
            if (searchTerm.Contains("open.spotify.com/"))
            {
                if (searchTerm.Contains("/playlist/"))
                    return new List<IPlayableItem>(await Playlist.Get(searchTerm.Split("playlist/").Last().Split("?")[0]));

                if (searchTerm.Contains("/track"))
                    return new List<IPlayableItem> {await Track.Get(searchTerm)};
                
                if (searchTerm.Contains("/album"))
                    return new List<IPlayableItem>(await Playlist.GetAlbum(searchTerm.Split("album/").Last().Split("?")[0]));
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
            {
                var ser = await Vbox7.SearchClient.SearchUrl(searchTerm);
                var obj = ser.ToVbox7Video();
                return new List<IPlayableItem>
                {
                    obj
                };
            }
            if (searchTerm.Contains("twitch.tv/"))
                return new List<IPlayableItem>
                {
                    new TwitchLiveStream
                    {
                        Url = searchTerm
                    }
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
                return Files.Get(searchTerm[7..]);
            if (searchTerm.StartsWith("vb7:"))
            {
                var res = await Vbox7.SearchClient.GetResultsFromSearch(searchTerm[4..]);
                await Debug.WriteAsync($"Objects are: {res.Count}");
                foreach (var el in res)
                {
                    await Debug.WriteAsync($"Element is: {el.Options}");
                }
                return new List<IPlayableItem>
                {
                    res.First().ToVbox7Video()
                };
            }
            if (searchTerm.StartsWith("pl:"))
                return SharePlaylist.Get(searchTerm[3..]);
            return new List<IPlayableItem>
            {
                await new Video().Search(searchTerm, length: length)
            };
        }

        public async Task<List<IPlayableItem>> Get(string searchTerm, List<DiscordAttachment> attachments, ulong guild)
        {
            var list = new List<IPlayableItem>();
            list.AddRange(await Attachments.GetAttachments(attachments, guild));
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