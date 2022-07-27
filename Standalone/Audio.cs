using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Methods;
using Microsoft.AspNetCore.Mvc;
using YtPlaylist = DiscordBot.Audio.Platforms.Youtube.Playlist;

namespace DiscordBot.Standalone
{
    public class Audio : Controller
    {
        public async Task<FileStreamResult> DownloadTrack(string id, int bitrate = 96,
            bool useOpus = true)
        {
            try
            {
                if (Bot.DebugMode)
                    await Debug.WriteAsync("Using Audio Controller");
                var res = await DiscordBot.Audio.Platforms.Search.Get(id);
                var first = res.First();
                await first.Download();
                var file = first.GetLocation();
                FfMpeg2 ff = new();
                return File(useOpus switch
                {
                    true => ff.Convert(file, codec: "-c:a libopus", addParameters: $"-b:a {bitrate}k"),
                    false => ff.Convert(file, codec: "-c:a libvorbis", addParameters: $"-b:a {bitrate}k")
                }, "audio/ogg", "audio.ogg", true);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"DownloadTrack failed with exception: \"{e}\"");
                throw;
            }
        }

        public async Task<JsonResult> Search(string term)
        {
            var items = await DiscordBot.Audio.Platforms.Search.Get(term, returnAllResults: true);
            return Json(items.Select(r => r.ToSearchResult()), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });
        }

        public FileStreamResult GetRandomDownload()
        {
            var files = Directory.EnumerateFiles($"{Bot.WorkingDirectory}/dll/audio").ToList();
            var rng = new Random();
            var rnFile = rng.Next(files.Count);
            FfMpeg2 ff = new();
            return File(ff.Convert(files[rnFile]), "audio/ogg", $"{files[rnFile][..^5].Split("/").Last()}.ogg");
        }
    }
}