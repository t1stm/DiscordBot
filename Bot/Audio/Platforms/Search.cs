#nullable enable
using System;
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
                var ser = await Vbox7SearchClient.SearchUrl(searchTerm);
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

            var res = await HandleBotProtocols(searchTerm);
            if (res != null)
            {
                return new List<PlayableItem>{res};
            }

            if (searchTerm.StartsWith("pl:"))
                return await SharePlaylist.Get(searchTerm[3..]);

            return returnAllResults switch
            {
                false => new List<PlayableItem>
                {
                    await Video.Search(searchTerm, length: length)
                },
                true => new List<PlayableItem>(await Video.SearchAllResults(searchTerm, length))
            };
        }

        private static async Task<PlayableItem?> HandleBotProtocols(string search)
        {
            var split = search.Split("://");
            if (split.Length < 2) return null;
            switch (split[0])
            {
                case "yt":
                    return await Video.SearchById(split[1]);
                case "spt":
                    return await Track.Get(split[1]);
                case "file":
                    return File.GetInfo(split[1]);
                case "dis-att":
                    var splitted = split[1].Split("-");
                    return File.GetInfo(string.Join('-', splitted[1..]), ulong.Parse(splitted[0]));
                case "vb7":
                    var result = await Vbox7SearchClient.SearchUrl($"https://vbox7.com/play:{split[1]}");
                    return result.ToVbox7Video();
                case "onl":
                    return new OnlineFile
                    {
                        Location = split[1]
                    };
                case "tts":
                    return new TtsText(string.Join("://", split[1..]));
                case "twitch":
                    return new TwitchLiveStream
                    {
                        Url = $"https://twitch.tv/{split[1..]}"
                    };
            }
            
            return null;
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