using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Playlists.Music_Storage.Objects;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class MediaInfo
    {
        public static async Task<MusicInfo> GetInformation(string location)
        {
            var musicInfo = new MusicInfo();
            var program = Process.Start(new ProcessStartInfo
            {
                FileName = "mediainfo",
                Arguments = $"--Output=JSON \"{location}\"",
                RedirectStandardOutput = true
            });
            if (program == null) throw new NullReferenceException("MediaInfo process is null.");
            await program.WaitForExitAsync();
            var json = await JsonDocument.ParseAsync(program.StandardOutput.BaseStream);
            var media = json.RootElement.GetProperty("media");
            var infoArray = media.GetProperty("track");
            var general = infoArray[0];
            musicInfo.OriginalTitle = general.GetProperty("Title").GetString();
            musicInfo.OriginalAuthor = general.GetProperty("Performer").GetString();
            musicInfo.Length = (ulong) (general.GetProperty("Duration").GetDouble() * 60);
            return musicInfo;
        }
    }
}