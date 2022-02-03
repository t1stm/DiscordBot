using System.IO;
using System.Threading.Tasks;
using BatToshoRESTApp.Methods;
using Microsoft.AspNetCore.Mvc;

namespace BatToshoRESTApp.Standalone
{
    public class Audio : Controller
    {
        public async Task<FileStreamResult> DownloadTrack(string id, bool getRaw = true, int bitrate = 96, bool useOpus = true)
        {
            await Debug.WriteAsync("Using Audio Controller");
            var stream = new MemoryStream();
            FfMpeg2 ff = new();
            if (!getRaw || !useOpus)
                await ff.Convert($"{Bot.WorkingDirectory}/dll/audio/{id}.webm", "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis",  $"-b:a {bitrate}k").CopyToAsync(stream);
            else await ff.Convert($"{Bot.WorkingDirectory}/dll/audio/{id}.webm").CopyToAsync(stream);
            stream.Position = 0;
            return File(stream, "audio/ogg", "stream.ogg");
        }
    }
}