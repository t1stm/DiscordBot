#nullable enable
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Playlists.Music_Storage.Objects;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class MediaInfo
    {
        public static async Task<MusicInfo> GetInformation(string location)
        {
            await Debug.WriteAsync($"MediaInfo: Checking \"{location}\"");
            var musicInfo = new MusicInfo();
            var program = Process.Start(new ProcessStartInfo
            {
                FileName = "mediainfo",
                Arguments = $"--Output=JSON \"{location}\"",
                RedirectStandardOutput = true
            });
            if (program == null) return musicInfo;
            await program.WaitForExitAsync();
            var json = await JsonDocument.ParseAsync(program.StandardOutput.BaseStream);
            if (!json.RootElement.TryGetProperty("media", out var media)) return musicInfo;
            if (!media.TryGetProperty("track", out var infoArray)) return musicInfo;
            var isAudio = infoArray.GetArrayLength() == 2;
            var general = isAudio ? infoArray[1] : infoArray[0];
            if (isAudio)
            {
                if (general.TryGetProperty("Title", out var title))
                    musicInfo.OriginalTitle = title.GetString();
                if (general.TryGetProperty("Performer", out var author))
                    musicInfo.OriginalAuthor = author.GetString();
            }
            musicInfo.Length = (ulong) (double.Parse(general.GetProperty("Duration").GetString() ?? "0") * 1000);
            return musicInfo;
        }
    }
}