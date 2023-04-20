using CustomPlaylistFormat.Objects;
using DiscordBot.Playlists;

namespace TestingApp;

public static class M3U8Exporter
{
    public static async void Export(Playlist playlist)
    {
        var file_stream = File.Open("/home/kris/Desktop/export.m3u8", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        var playlist_text_builder = new StreamWriter(file_stream);
        
        await playlist_text_builder.WriteLineAsync("#EXTM3U");

        var parsed = await PlaylistManager.ParsePlaylist(playlist);
        
        foreach (var playableItem in parsed)
        {
            await playlist_text_builder.WriteLineAsync(playableItem.GetLocation());
        }

        await playlist_text_builder.FlushAsync();
        await playlist_text_builder.DisposeAsync();
        
        file_stream.Close();
    }
}