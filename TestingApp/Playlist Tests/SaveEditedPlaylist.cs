using System.Text.Json;
using CustomPlaylistFormat;
using CustomPlaylistFormat.Objects;

namespace TestingApp;

public class SaveEditedPlaylist
{
    public async Task Execute()
    {
        var info = new PlaylistInfo
        {
            Name = "Мазен Тираджия",
            Maker = "t1stm",
            Description = "Този плейлист съдържа най-мазните парчета направени в периода 1980-2010г."
        };
        await using var file = File.Open("./newplay.json", FileMode.Open);
        var entries = JsonSerializer.Deserialize<List<Entry>>(file)!;

        var newFile = File.Open("./newplay.play", FileMode.Create);
        var encoder = new Encoder(newFile, info);
        encoder.Encode(entries.ToArray());

        await newFile.FlushAsync();
    }
}