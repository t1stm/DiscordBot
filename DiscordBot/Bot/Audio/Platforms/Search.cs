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
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Methods;
using DiscordBot.Playlists;
using DiscordBot.Playlists.Music_Storage;
using DSharpPlus.Entities;
using Playlist = DiscordBot.Audio.Platforms.Spotify.Playlist;

namespace DiscordBot.Audio.Platforms
{
    public static class Search
    {
        public static async Task<List<PlayableItem>?> Get(string? searchTerm, ulong length = 0,
            bool returnAllResults = false, Action<string>? onError = null)
        {
            await Debug.WriteAsync($"Search term is: \"{searchTerm}\"");
            if (searchTerm == null) return null;
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

            if (searchTerm.Contains($"playlists.{Bot.MainDomain}/"))
            {
                return await PlaylistManager.FromLink(searchTerm, onError);
            }
            
            if (searchTerm.StartsWith("http") || searchTerm.StartsWith("https"))
                return new List<PlayableItem>
                {
                    new OnlineFile
                    {
                        Location = searchTerm
                    }
                };

            var res = await SearchBotProtocols(searchTerm);
            if (res != null)
            {
                switch (res)
                {
                    case List<PlayableItem> list:
                        return list;
                    case PlayableItem item:
                        return new List<PlayableItem> {item};
                }
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

        public static async Task<object?> SearchBotProtocols(string search)
        {
            var split = search.Split("://");
            if (split.Length < 2) return null;
            switch (split[0])
            {
                case "yt":
                    return await Video.SearchById(split[1]);
                case "yt-ov":
                    return YoutubeOverride.FromId(split[1]);
                case "spt":
                    return await Track.Get(split[1], true);
                case "file":
                    return Files.Get(split[1]);
                case "dis-att":
                    var splitted = split[1].Split("-");
                    return File.GetInfo(string.Join('-', splitted[1..]), ulong.Parse(splitted[0]));
                case "vb7":
                    var result = await Vbox7SearchClient.SearchUrl($"https://vbox7.com/play:{split[1]}");
                    return result.ToVbox7Video();
                case "audio":
                    if (split[1] != "*") return MusicManager.SearchById(split[1])?.ToMusicObject();
                    var audios = MusicManager.GetAll();
                    List<PlayableItem> audioItems = new();
                    audioItems.AddRange(audios.Select(r => r.ToMusicObject()));
                    return audioItems;
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

        public static async Task<List<PlayableItem>?> Get(string searchTerm, List<DiscordAttachment>? attachments,
            ulong? guild)
        {
            if (attachments == null || attachments.Count < 1) return await Get(searchTerm);
            var list = new List<PlayableItem>();
            list.AddRange(await Attachments.GetAttachments(attachments, guild ?? 0) ?? Enumerable.Empty<PlayableItem>());
            return list;
        }

        public static async Task<List<PlayableItem>> Get(SpotifyTrack track, bool urgent = false)
        {
            return new()
            {
                await Video.Search(track, urgent)
            };
        }
    }
}