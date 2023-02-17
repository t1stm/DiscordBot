using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage.Objects;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class ExtractImages
    {
        public const string ExportDirectory = "/srv/http/Album_Covers";
        public static void Run(bool isVerbose = false)
        {
            if (!Directory.Exists(ExportDirectory)) Directory.CreateDirectory(ExportDirectory);
            foreach (var genreDirectory in Directory.GetDirectories(MusicManager.WorkingDirectory))
            {
                foreach (var artistDirectory in Directory.GetDirectories(genreDirectory))
                {
                    ParseFolder(artistDirectory, isVerbose);
                }
            }
        }

        public static void ParseFolder(string folder, bool isVerbose = false)
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
            
                if (isVerbose) Debug.Write($"File: \"{info.RelativeLocation}\" Hash: \"{hashString}\"");
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