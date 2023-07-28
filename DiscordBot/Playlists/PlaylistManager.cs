#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CustomPlaylistFormat;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Methods;
using Result;
using Result.Objects;

namespace DiscordBot.Playlists;

public static class PlaylistManager
{
    public static readonly string PlaylistDirectory = $"{Bot.WorkingDirectory}/NewPlaylists";

    public static List<PlaylistInfo> Infos { get; set; } = new();

    public static void LoadPlaylistInfos()
    {
        try
        {
            Infos = new List<PlaylistInfo>();
            var items = Directory.GetFiles(PlaylistDirectory);
            foreach (var item in items)
            {
                var split = item.Split('/');
                var chunk = split[^1][..^5]; // Last element.
                if (!Guid.TryParse(chunk, out var guid)) continue;
                Debug.Write($"Loading playlist: \"{guid.ToString()}\"");
                var playlist = GetDataIfExists(guid);
                if (playlist?.Info == null) continue;
                if (File.Exists($"{PlaylistThumbnail.WorkingDirectory}/Thumbnail Images/{guid}.png"))
                    playlist.Value.Info.HasThumbnail = true;
                Infos.Add(playlist.Value.Info);
            }
        }
        catch (Exception e)
        {
            Debug.Write($"Unable to load playlists. \"{e}\"");
        }
    }

    public static IEnumerable<PlaylistInfo> GetAll()
    {
        List<PlaylistInfo> infos;

        lock (Infos)
        {
            infos = Infos.ToList();
        }

        return infos;
    }

    private static PlaylistInfo? GetInMemory(Guid guid)
    {
        lock (Infos)
        {
            return Infos.AsParallel().FirstOrDefault(r => r.Guid == guid);
        }
    }

    public static async Task<Result<List<PlayableItem>, Error>> FromLink(string link)
    {
        var split = link.Split('/');
        if (split.Length < 2)
            return Result<List<PlayableItem>, Error>.Error(
                new PlaylistManagerError(PlaylistManagerErrorType.InvalidUrl));

        return await FromString(split[^1]);
    }

    public static async Task<Result<List<PlayableItem>, Error>> FromString(string guidText)
    {
        if (!Guid.TryParse(guidText, out var guid))
        {
            await Debug.WriteAsync($"Playlist with id \'{guidText}\' not found.");
            return Result<List<PlayableItem>, Error>.Error(
                new PlaylistManagerError(PlaylistManagerErrorType.InvalidRequest));
        }

        var data = GetIfExists(guid);
        return data is not null
            ? Result<List<PlayableItem>, Error>.Success(await ParsePlaylist(data.Value))
            : Result<List<PlayableItem>, Error>.Error(new PlaylistManagerError(PlaylistManagerErrorType.NotFound));
    }

    public static async Task<List<PlayableItem>> ParsePlaylist(Playlist playlist)
    {
        List<PlayableItem> items = new();
        if (playlist.PlaylistItems == null) return items;
        foreach (var entry in playlist.PlaylistItems)
        {
            var video = $"{ItemTypeToString(entry.Type)}://{entry.Data}";
            var search = await Search.SearchBotProtocols(video);
            if (search == null) continue;
            var parsed = Search.ParseObject(search);

            if (parsed != Status.OK) continue;
            items.AddRange(parsed.GetOK());
        }

        return items;
    }

    public static Playlist? GetDataIfExists(Guid guid)
    {
        var filename = $"{PlaylistDirectory}/{guid.ToString()}.play";
        if (!File.Exists(filename)) return null;

        var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = new Decoder(file);
        var read = decoder.Read(ReadMode.InfoOnly);
        var inMemory = GetInMemory(guid);
        read.Info = inMemory ?? read.Info;
        if (read.Info == null) return read;

        read.Info.Guid = guid;
        read.Info.HasThumbnail = File.Exists($"{PlaylistThumbnail.WorkingDirectory}/Thumbnail Images/{guid}.png");
        return read;
    }

    public static Playlist? GetIfExists(Guid guid)
    {
        var filename = $"{PlaylistDirectory}/{guid.ToString()}.play";
        if (!File.Exists(filename)) return null;

        var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = new Decoder(file);
        var read = decoder.Read();

        var inMemory = GetInMemory(guid);
        read.Info = inMemory ?? read.Info;
        if (read.Info == null) return read;

        read.Info.Guid = guid;
        read.Info.HasThumbnail = File.Exists($"{PlaylistThumbnail.WorkingDirectory}/Thumbnail Images/{guid}.png");
        return read;
    }

    public static Guid GetGuid()
    {
        return Guid.NewGuid();
    }

    public static Playlist? SavePlaylist(IEnumerable<PlayableItem> list, PlaylistInfo info)
    {
        var guid = GetGuid();
        try
        {
            return SavePlaylist(list, info, guid);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Playlist? SavePlaylist(IEnumerable<PlayableItem> list, PlaylistInfo info, Guid guid)
    {
        var fileStream = File.Open($"{PlaylistDirectory}/{guid.ToString()}.play", FileMode.Create, FileAccess.ReadWrite,
            FileShare.ReadWrite);
        try
        {
            var playlistEncoder = new Encoder(fileStream, info);

            var entries = list.Select(r => new Entry
            {
                Type = GetItemType(r),
                Data = string.Join("://", r.GetAddUrl().Split("://")[1..])
            });

            var items = entries as Entry[] ?? entries.ToArray();
            playlistEncoder.Encode(items);
            info.Guid = guid;
            return new Playlist
            {
                FailedToParse = false,
                PlaylistItems = items,
                Info = info
            };
        }
        catch (Exception e)
        {
            Debug.Write($"Save Playlist failed: \"{e}\"");
        }
        finally
        {
            fileStream.Close();
        }

        return null;
    }

    public static byte GetItemType(PlayableItem it)
    {
        return it switch
        {
            YoutubeVideoInformation => 01,
            SpotifyTrack => 02,
            SystemFile => 03,
            Vbox7Video => 05,
            OnlineFile => 06,
            TtsText => 07,
            TwitchLiveStream => 08,
            YoutubeOverride => 09,
            MusicObject => 10,
            _ => 255
        };
    }

    public static string ItemTypeToString(byte it)
    {
        return it switch
        {
            01 => "yt",
            02 => "spt",
            03 => "file",
            04 => "dis-att",
            05 => "vb7",
            06 => "onl",
            07 => "tts",
            08 => "ttv",
            09 => "yt-ov",
            10 => "audio",
            _ => ""
        };
    }
}