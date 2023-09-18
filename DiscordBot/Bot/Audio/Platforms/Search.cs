#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
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
using Result;
using Result.Objects;
using Playlist = DiscordBot.Audio.Platforms.Spotify.Playlist;

namespace DiscordBot.Audio.Platforms;

public static class Search
{
    public static async Task<Result<List<PlayableItem>, Error>> Get(string? searchTerm, ulong length = 0,
        bool returnAllResults = false)
    {
        try
        {
            await Debug.WriteAsync($"Search term is: \"{searchTerm}\"");
            if (string.IsNullOrEmpty(searchTerm))
            {
                await Debug.WriteAsync("Search term is null.");
                return Result<List<PlayableItem>, Error>.Error(new NullError(NullType.SearchTerm));
            }

            if (searchTerm.Contains("open.spotify.com/"))
            {
                if (searchTerm.Contains("/playlist/"))
                {
                    var req = await Playlist.Get(searchTerm.Split("playlist/").Last().Split("?")[0]);
                    return req;
                }

                if (searchTerm.Contains("/track"))
                    return ToList(await Track.Get(searchTerm));

                if (searchTerm.Contains("/album"))
                    return await Playlist.GetAlbum(searchTerm.Split("album/").Last().Split("?")[0]);
            }

            if (searchTerm.Contains("youtube.com/"))
            {
                if (searchTerm.Contains("playlist?list="))
                {
                    var pl = await Youtube.Playlist.Get(searchTerm);
                    return pl;
                }

                if (searchTerm.Contains("watch?v="))
                    return ToList(await Video.SearchById(searchTerm.Split("watch?v=").Last().Split("&")[0]));
                if (searchTerm.Contains("shorts/"))
                    return ToList(await Video.SearchById(searchTerm.Split("shorts/").Last().Split("&")[0]));
            }

            if (searchTerm.Contains("youtu.be/"))
                return ToList(await Video.SearchById(searchTerm.Split("youtu.be/").Last().Split("&")[0]));
            if (searchTerm.Contains("http") && searchTerm.Contains("vbox7.com"))
            {
                var ser = await Vbox7SearchClient.SearchUrl(searchTerm);
                if (ser != Status.OK) return Result<List<PlayableItem>, Error>.Error(new Vbox7Error());
                var obj = ser.GetOK().ToVbox7Video();
                return ToList(Result<PlayableItem, Error>.Success(obj));
            }

            if (searchTerm.Contains("twitch.tv/"))
                return ToList(Result<PlayableItem, Error>.Success(new TwitchLiveStream
                {
                    Url = searchTerm
                }));

            if (searchTerm.Contains($"playlists.{Bot.MainDomain}/"))
            {
                await Debug.WriteAsync("Playlist URL.");
                return await PlaylistManager.FromLink(searchTerm);
            }

            if (searchTerm.StartsWith("http") || searchTerm.StartsWith("https"))
                return ToList(Result<PlayableItem, Error>.Success(new OnlineFile
                {
                    Location = searchTerm
                }));

            var res = await SearchBotProtocols(searchTerm);
            if (res != null)
            {
                var parsed = ParseObject(res);
                if (parsed == Status.OK)
                    return parsed;
            }

            if (searchTerm.StartsWith("pl:"))
                return await SharePlaylist.Get(searchTerm[3..]);

            var databaseItem = MusicManager.SearchOneByTerm(searchTerm);

            return returnAllResults switch
            {
                true when databaseItem is not null =>
                    AddDatabaseItemToResults(databaseItem.ToMusicObject(),
                        await Video.SearchAllResults(searchTerm, length)),
                true => await Video.SearchAllResults(searchTerm, length),
                false when databaseItem is not null =>
                    ToList(Result<PlayableItem, Error>.Success(databaseItem.ToMusicObject())),
                false => ToList(await Video.Search(searchTerm, length: length))
            };
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Exception thrown when using search: \"{e}\"");
            return Result<List<PlayableItem>, Error>.Error(new SearchError(e.Message));
        }
    }

    public static Result<List<PlayableItem>, Error> ParseObject(object? res)
    {
        return res switch
        {
            Result<List<PlayableItem>, Error> list => list,
            Result<PlayableItem, Error> item => ToList(item),
            _ => Result<List<PlayableItem>, Error>.Error(new NoResultsError())
        };
    }

    private static Result<List<PlayableItem>, Error> AddDatabaseItemToResults(PlayableItem item,
        Result<List<PlayableItem>, Error> list)
    {
        if (list != Status.OK) return list;
        var ok = list.GetOK();
        ok.Insert(0, item);
        return list;
    }

    private static Result<List<PlayableItem>, Error> ToList(Result<PlayableItem, Error> item)
    {
        return item != Status.OK
            ? Result<List<PlayableItem>, Error>.Error(item.GetError())
            : Result<List<PlayableItem>, Error>.Success(new List<PlayableItem>
            {
                item.GetOK()
            });
    }

    public static async Task<object?> SearchBotProtocols(string search)
    {
        var split = search.Split("://");
        if (split.Length < 2) return null;
        var remainder = string.Join("://", split[1..]);
        switch (split[0])
        {
            case "yt":
                return await Video.SearchById(split[1]);
            case "yt-ov":
                var ov = YoutubeOverride.FromId(split[1]);
                return ov == null
                    ? Result<PlayableItem, Error>.Error(new NullError(NullType.Override))
                    : Result<PlayableItem, Error>.Success(ov);
            case "spt":
                return await Track.Get(split[1], true);
            case "file":
                return Files.Get(split[1]);
            case "dis-att":
                var splitted = split[1].Split("-");
                return File.GetInfo(string.Join('-', splitted[1..]), ulong.Parse(splitted[0]));
            case "vb7":
                var ser = await Vbox7SearchClient.SearchUrl($"https://vbox7.com/play:{split[1]}");
                if (ser != Status.OK) return Result<List<PlayableItem>, Error>.Error(new Vbox7Error());
                return ser;
            case "audio":
                List<PlayableItem> audioItems;
                if (split[1] != "*")
                {
                    var foundById = MusicManager.SearchById(split[1])?.ToMusicObject();
                    if (foundById != null) return Result<PlayableItem, Error>.Success(foundById);
                    var patternSearch = MusicManager.SearchByPattern(split[1]).ToList();
                    audioItems = new List<PlayableItem>();
                    audioItems.AddRange(patternSearch.Select(r => r.ToMusicObject()));
                    return audioItems.Count > 1
                        ? Result<List<PlayableItem>, Error>.Success(audioItems)
                        : Result<List<PlayableItem>, Error>.Error(new NoResultsError());
                }

                var audios = MusicManager.GetAll();
                audioItems = new List<PlayableItem>();
                audioItems.AddRange(audios.Select(r => r.ToMusicObject()));
                return audioItems.Count > 1
                    ? Result<List<PlayableItem>, Error>.Success(audioItems)
                    : Result<List<PlayableItem>, Error>.Error(new NoResultsError());
            case "onl":
                return Result<PlayableItem, Error>.Success(new OnlineFile
                {
                    Location = split[1]
                });
            case "tts":
                return Result<PlayableItem, Error>.Success(new TtsText(remainder));
            case "twitch":
                return Result<PlayableItem, Error>.Success(new TwitchLiveStream
                {
                    Url = $"https://twitch.tv/{remainder}"
                });
            case "playlist":
                return PlaylistManager.FromString(remainder);
        }

        return null;
    }

    public static async Task<Result<List<PlayableItem>, Error>> Get(string searchTerm,
        List<DiscordAttachment>? attachments,
        ulong? guild)
    {
        if (attachments == null || attachments.Count < 1) return await Get(searchTerm);
        return await Attachments.GetAttachments(attachments, guild ?? 0);
    }

    public static async Task<Result<PlayableItem, Error>> GetSingle(SpotifyTrack track)
    {
        var result = MusicManager.SearchFromSpotify(track);
        if (result == null) return await Video.Search(track);
        return Result<PlayableItem, Error>.Success(result.ToMusicObject());
    }
}