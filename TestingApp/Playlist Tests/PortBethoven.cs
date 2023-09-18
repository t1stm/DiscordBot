using CustomPlaylistFormat.Objects;
using DiscordBot.Playlists;
using DiscordBot.Playlists.Music_Storage;

namespace TestingApp;

public static class PortBethoven
{
    public static void Test()
    {
        var file = File.ReadAllLines($"{MusicManager.WorkingDirectory}/Селекция от Бетовен.m3u8");

        file = file[1..];

        var all_songs = MusicManager.GetAll();
        
        var songs = file.Select(r => all_songs.First(w => w.RelativeLocation == r)).ToArray();
        var items = songs.Select(r => r.ToMusicObject());
        
        PlaylistManager.SavePlaylist(items, new PlaylistInfo
        {
            Name = "Селекция от Бетовен",
            Description = "\"Музиката е... по-висше послание от всяка мъдрост и философия.\" - Бетовен",
            Maker = "t1stm"
        }, Guid.Parse("FDDABD10-3467-4960-AFB0-176A83D2DAD0"));
    }
}