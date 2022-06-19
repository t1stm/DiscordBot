using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms.Discord;
using DiscordBot.Audio.Platforms.Local;
using DiscordBot.Audio.Platforms.Spotify;
using DiscordBot.Audio.Platforms.Vbox7;
using DiscordBot.Methods;
using DSharpPlus.Entities;
using Playlist = DiscordBot.Audio.Platforms.Spotify.Playlist;
using Video = DiscordBot.Audio.Platforms.Youtube.Video;

namespace DiscordBot.Audio.Platforms
{
    public class Search
    {
        public static async Task<List<PlayableItem>> Get(string searchTerm, ulong length = 0,
            bool returnAllResults = false)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Search term is: \"{searchTerm}\"");
            if (searchTerm.Contains("open.spotify.com/"))
            {
                if (searchTerm.Contains("/playlist/"))
                    return new List<PlayableItem>(
                        await Playlist.Get(searchTerm.Split("playlist/").Last().Split("?")[0]));

                if (searchTerm.Contains("/track"))
                    return new List<PlayableItem> {await Track.Get(searchTerm)};

                if (searchTerm.Contains("/album"))
                    return new List<PlayableItem>(
                        await Playlist.GetAlbum(searchTerm.Split("album/").Last().Split("?")[0]));
            }

            if (searchTerm.Contains("youtube.com/"))
            {
                if (searchTerm.Contains("playlist?list="))
                {
                    var pl = await Youtube.Playlist.Get(searchTerm);
                    return new List<PlayableItem>(pl);
                }

                if (searchTerm.Contains("watch?v="))
                    return new List<PlayableItem>
                    {
                        await Video.SearchById(searchTerm.Split("watch?v=").Last().Split("&")[0])
                    };
                if (searchTerm.Contains("shorts/"))
                    return new List<PlayableItem>
                    {
                        await Video.SearchById(searchTerm.Split("shorts/").Last().Split("&")[0])
                    };
            }

            if (searchTerm.Contains("youtu.be/"))
                return new List<PlayableItem>
                {
                    await Video.SearchById(searchTerm.Split("youtu.be/").Last().Split("&")[0])
                };
            if (searchTerm.Contains("http") && searchTerm.Contains("vbox7.com"))
            {
                var ser = await SearchClient.SearchUrl(searchTerm);
                var obj = ser.ToVbox7Video();
                return new List<PlayableItem>
                {
                    obj
                };
            }

            if (searchTerm.Contains("twitch.tv/"))
                return new List<PlayableItem>
                {
                    new TwitchLiveStream
                    {
                        Url = searchTerm
                    }
                };
            if (searchTerm.StartsWith("http") || searchTerm.StartsWith("https"))
                return new List<PlayableItem>
                {
                    new OnlineFile
                    {
                        Location = searchTerm
                    }
                };

            if (searchTerm.StartsWith("file://"))
                return Files.Get(searchTerm[7..]);
            if (searchTerm.StartsWith("vb7:"))
            {
                var res = await SearchClient.GetResultsFromSearch(searchTerm[4..]);
                await Debug.WriteAsync($"Objects are: {res.Count}");
                foreach (var el in res) await Debug.WriteAsync($"Element is: {el.Options}");
                return returnAllResults switch
                {
                    false => new List<PlayableItem>
                    {
                        res.First().ToVbox7Video()
                    },
                    true => new List<PlayableItem>(res.Select(r => r.ToVbox7Video()))
                };
            }

            if (searchTerm.StartsWith("pl:"))
                return SharePlaylist.Get(searchTerm[3..]);

            return returnAllResults switch
            {
                false => new List<PlayableItem>
                {
                    await Video.Search(searchTerm, length: length)
                },
                true => new List<PlayableItem>(await Video.SearchAllResults(searchTerm, length))
            };
        }

        public static async Task<List<PlayableItem>> Get(string searchTerm, List<DiscordAttachment> attachments,
            ulong guild)
        {
            var list = new List<PlayableItem>();
            list.AddRange(await Attachments.GetAttachments(attachments, guild));
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3) return list;
            list.AddRange(await Get(searchTerm));
            return list;
        }

        public static async Task<List<YoutubeVideoInformation>> Get(SpotifyTrack track, bool urgent = false)
        {
            return new()
            {
                await Video.Search(track, urgent)
            };
        }
    }
}