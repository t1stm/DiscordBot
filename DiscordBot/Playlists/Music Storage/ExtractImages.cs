using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage.Objects;
using Newtonsoft.Json;

namespace DiscordBot.Playlists.Music_Storage;

public static class ExtractImages
{
    public const string ExportDirectory = $"/srv/http/{Bot.MainDomain}/Album_Covers/";

    public static void Run(bool isVerbose = false)
    {
        if (!Directory.Exists(ExportDirectory)) Directory.CreateDirectory(ExportDirectory);
        foreach (var genreDirectory in Directory.GetDirectories(MusicManager.WorkingDirectory))
        foreach (var artistDirectory in Directory.GetDirectories(genreDirectory))
            ParseFolder(artistDirectory, isVerbose);
    }

    public static void ParseFolder(string folder, bool isVerbose = false)
    {
        var serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };
        using var file_stream = File.Open($"{folder}/Info.json", FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using var reader = new StreamReader(file_stream, Encoding.UTF8, true, 1024, true);

        var json = reader.ReadToEnd();
        var items = JsonConvert.DeserializeObject<List<MusicInfo>>(json) ?? Enumerable.Empty<MusicInfo>().ToList();

        foreach (var info in items)
        {
            var location = info.ToMusicObject().GetLocation();
            var image = Flac.GetImageFromFile(location);

            if (!image.HasData)
            {
                image = Id3v2.GetImageFromTag(location);
                if (!image.HasData) continue;
            }

            var hashString = Sha1Generator.Get(image.Data!);
            var extension = Flac.GetImageFiletype(image.Data!);

            if (isVerbose) Debug.Write($"File: \"{info.RelativeLocation}\" Hash: \"{hashString}\"");
            var filename = $"{ExportDirectory}/{hashString}.{extension}";
            info.CoverUrl = $"$[DOMAIN]/{hashString}.{extension}";
            if (File.Exists(filename)) continue;
            File.WriteAllBytes(filename, image.Data!);
        }

        file_stream.Position = 0;

        using var writer = new StreamWriter(file_stream, Encoding.UTF8);
        serializer.Serialize(writer, items);
    }
}