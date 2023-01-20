using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage;
using DiscordBot.Playlists.Music_Storage.Objects;

namespace TestingApp.Music_Database_Tests
{
    public static class ExtractId3v2Images
    {
        public const string ExportDirectory = "/srv/http/Album_Covers";
        public static void Test()
        {
            if (!Directory.Exists(ExportDirectory)) Directory.CreateDirectory(ExportDirectory);
            foreach (var genreDirectory in Directory.GetDirectories(MusicManager.WorkingDirectory))
            {
                foreach (var artistDirectory in Directory.GetDirectories(genreDirectory))
                {
                    ParseFolder(artistDirectory);
                }
            }
        }

        public static void ParseFolder(string folder)
        {
            using var stream = File.Open($"{folder}/Info.json", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            var items = JsonSerializer.Deserialize<List<MusicInfo>>(stream) ?? Enumerable.Empty<MusicInfo>().ToList();
            foreach (var info in items)
            {
                var location = info.ToMusicObject().GetLocation();
                var image = Flac.GetImageFromFile(location);

                if (!image.HasData)
                {
                    continue;
                }

                var hashString = Sha1Generator.Get(image.Data!);
                var extension = Flac.GetImageFiletype(image.Data!);
            
                Debug.Write($"File: \"{info.RelativeLocation}\" Hash: \"{hashString}\"");
                var filename = $"{ExportDirectory}/{hashString}.{extension}";
                info.CoverUrl = $"$[DOMAIN]/{hashString}.{extension}";
                if (File.Exists(filename)) continue;
                File.WriteAllBytes(filename, image.Data!);
            }

            stream.Position = 0;
            JsonSerializer.Serialize(stream, items);
            stream.Flush();
            stream.Dispose();
        }
        
    }
}