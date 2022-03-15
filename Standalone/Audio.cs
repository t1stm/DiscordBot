using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Methods;
using Microsoft.AspNetCore.Mvc;

namespace BatToshoRESTApp.Standalone
{
    public class Audio : Controller
    {
        public async Task<FileStreamResult> DownloadTrack(string id, bool getRaw = true, int bitrate = 96, bool useOpus = true)
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
            var search = new Search();
            var res = await search.Get(id.Contains("http") ? id : $"https://youtube.com/watch?v={id}");
            try
            {
                
            }
            catch (Exception)
            {
                //Ignored
            }
            file = res[0].GetLocation();
            return File((!getRaw || !useOpus) switch
            {
                true => ff.Convert(file, "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis",  $"-b:a {bitrate}k"),
                false => ff.Convert(file)
            }, "audio/ogg", "audio.ogg", true);
        }

        public async Task<JsonResult> Search(string term)
        {
            var search = new Search();
            var res = await search.Get(term);
            return Json(res.Cast<YoutubeVideoInformation>().ToList());
        }

        public FileStreamResult GetRandomDownload()
        {
            var files = Directory.EnumerateFiles($"{Bot.WorkingDirectory}/dll/audio").ToList();
            var rng = new Random();
            var rnFile = rng.Next(files.Count);
            FfMpeg2 ff = new();
            return File(ff.Convert(files[rnFile]), "audio/ogg", "random.ogg");
        }
    }
}