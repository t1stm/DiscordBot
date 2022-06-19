using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Methods;
using Microsoft.AspNetCore.Mvc;
using YtPlaylist = DiscordBot.Audio.Platforms.Youtube.Playlist;

namespace DiscordBot.Standalone
{
    public class Audio : Controller
    {
        public async Task<FileStreamResult> DownloadTrack(string id, bool getRaw = true, int bitrate = 96,
            bool useOpus = true)
        {
            await Debug.WriteAsync("Using Audio Controller");
            FfMpeg2 ff = new();
            var file = $"{Bot.WorkingDirectory}/dll/audio/{id}.webm";
            if (System.IO.File.Exists(file))
                return File((!getRaw || !useOpus) switch
                {
                    true => ff.Convert(file, "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis", $"-b:a {bitrate}k"),
                    false => ff.Convert(file)
                }, "audio/ogg", "audio.ogg", true);
            var res = await DiscordBot.Audio.Platforms.Search.Get(id.StartsWith("http")
                ? id
                : $"https://youtube.com/watch?v={id}");
            file = res[0].GetLocation();
            return File((!getRaw || !useOpus) switch
            {
                true => ff.Convert(file, "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis", $"-b:a {bitrate}k"),
                false => ff.Convert(file)
            }, "audio/ogg", "audio.ogg", true);
        }

        public async Task<JsonResult> Search(string term)
        {
            var items = await DiscordBot.Audio.Platforms.Search.Get(term, returnAllResults: true);
            return Json(items.Select(r => r.ToSearchResult()));
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