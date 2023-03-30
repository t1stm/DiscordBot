#nullable enable
using System.Text.Json;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Audio.Platforms;
using DiscordBot.Audio.Platforms.Local;
using DiscordBot.Methods;
using DiscordBot.Playlists;
using File = System.IO.File;
using Result.Objects;

namespace TestingApp;

public class EditPlaylist
{
    public async Task Execute()
    {
        var playlist = PlaylistManager.GetIfExists(Guid.Parse("e30e0c85-e8c2-4d7c-848c-55ef32cca153"));
        if (playlist is null) throw new NullReferenceException();
        var newPlaylist = new Playlist
        {
            Info = new PlaylistInfo
            {
                Name = "Мазен Тираджия",
                Maker = "t1stm",
                Description = "Този плейлист съдържа най-мазните парчета направени в периода 1980-2010г."
            }
        };

        List<Entry> entries = new();

        foreach (var entry in playlist.Value.PlaylistItems!)
        {
            var item = entry;
            await Debug.WriteAsync($"({item.Type}) - \"{item.Data}\"");
            var search = await Search.Get(
                $"{PlaylistManager.ItemTypeToString(item.Type)}://{item.Data}");
            var videos = search == Status.OK ? search.GetOK() : Enumerable.Empty<PlayableItem>().ToList();

            var first = videos.FirstOrDefault();
            if (first is null) throw new NullReferenceException();
            await Debug.WriteAsync($"Name: \"{first.GetName()}\"");
            await Debug.WriteAsync("Write nothing to add this to the list or write something to remove it.");
            item.Name = first.GetName();
            entries.Add(item);
        }

        var dirFiles = Files.Get("/nvme0/DiscordBot/dll/Overrides/");
        if (dirFiles != Status.OK) return;

        entries.AddRange(dirFiles.GetOK().Select(file =>
            new Entry
            {
                Type = PlaylistManager.GetItemType(file),
                Data = string.Join("://", file.GetAddUrl().Split("://")[1..]),
                Name = file.GetName()
            }));

        var random = new Random();
        newPlaylist.PlaylistItems = entries.DistinctBy(r => r.Data).OrderBy(_ => random.Next()).ToArray();

        var playlistFile = File.Open("./newplay.json", FileMode.Create);
        /*var encoder = new Encoder(playlistFile, newPlaylist.Info);
        encoder.Encode(newPlaylist.PlaylistItems);*/
        await JsonSerializer.SerializeAsync(playlistFile, newPlaylist.PlaylistItems);
        await playlistFile.FlushAsync();
    }
}