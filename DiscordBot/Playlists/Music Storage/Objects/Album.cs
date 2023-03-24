using System;
using System.Linq;

namespace DiscordBot.Playlists.Music_Storage.Objects;

public class Album
{
    public string AlbumName;
    public string Artist;
    public MusicInfo[] Songs;

    public Album()
    {
        RandomString = Bot.RandomString(2);
    }

    private string RandomString { get; }
    public string AccessString => $"{FillConcat(Artist, 2)}-{FillConcat(AlbumName, 6)}-{RandomString}";

    private static string FillConcat(string value, int length)
    {
        if (value.Length >= length) return value[..length];
        Span<char> modified = stackalloc char[length];
        for (var i = 0; i < value.Length; i++) modified[i] = value[i];

        for (var i = value.Length; i < length; i++) modified[i] = '_';

        return modified.ToString();
    }

    public static Album FromM3U(string location)
    {
        var album = new Album
        {
            AlbumName = string.Join('.', location.Split('/').Last().Split('.')[..^1])
        };

        var databaseItems = MusicManager.GetAll();
        var locations = PlaylistFileReader.ReadM3UFile(location);

        var songs = locations.Select(r =>
            databaseItems.FirstOrDefault(song => song.RelativeLocation?.EndsWith(r) ?? false));
        album.Songs = songs.ToArray();

        if (album.Songs.Any(r => r == null))
            throw new Exception($"A song from the playlist is null. Location: \'{location}\'");

        return album;
    }
}