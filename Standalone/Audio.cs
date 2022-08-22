using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Methods;
using Microsoft.AspNetCore.Mvc;
using YtPlaylist = DiscordBot.Audio.Platforms.Youtube.Playlist;

namespace DiscordBot.Standalone
{
    public class Audio : Controller
    {
        public static readonly List<EncodedAudio> EncodedAudio = new();
        public static readonly List<SocketSession> GeneratedSocketSessions = new();

        private const int AudioCacheTimeout = 15;

        public static void RemoveStale()
        {
            var now = DateTime.UtcNow.Ticks;
            lock (EncodedAudio)
            {
                foreach (var audio in EncodedAudio.Where(r => r.Expire < now).ToList())
                {
                    if (Bot.DebugMode)
                        Debug.Write(
                            $"Removing stale audio: \"{audio.SearchTerm}\" - Now: {now} - Expire: {audio.Expire} - Encoded: {audio.Encoded}");
                    EncodedAudio.Remove(audio);
                }
            }

            lock (GeneratedSocketSessions)
            {
                foreach (var session in GeneratedSocketSessions.Where(r => r.StartExpire < now).ToList())
                {
                    if (Bot.DebugMode)
                        Debug.Write(
                            $"Removing socket session id: \"{session.Id.ToString()}\" - Now: {now} - Expire: {session.StartExpire}");
                    GeneratedSocketSessions.Remove(session);
                }
            }
        }

        public static void PrintAudio(bool show = true)
        {
            lock (EncodedAudio)
            {
                if (!show) return;
                Debug.Write("Showing all encoded audios.");
                var now = DateTime.UtcNow.Ticks;
                foreach (var audio in EncodedAudio)
                {
                    Debug.Write(
                        $"\"{audio.SearchTerm}\" - Now: {now} - Expire: {audio.Expire} - Remaining: {new DateTime().AddTicks(audio.Expire - now > 0 ? audio.Expire - now : 0):HH:mm:ss} - Encoded: {audio.Encoded}");
                }
            }
        }

        public async Task<ActionResult> DownloadTrack(string id, int bitrate = 96, bool useOpus = true)
        {
            try
            {
                Response.StatusCode = 200;
                Response.ContentType = "audio/ogg";
                Response.Headers.ContentDisposition = "attachment; filename=audio.ogg; filename*=UTF-8''audio.ogg";
                Response.Headers.AcceptRanges = "bytes";

                if (Bot.DebugMode) await Debug.WriteAsync("Using Audio Controller");
                var res = await DiscordBot.Audio.Platforms.Search.Get(id);
                var first = res.First();
                FfMpeg2 ff = new();
                EncodedAudio foundEnc;
                lock (EncodedAudio)
                {
                    foundEnc = EncodedAudio.AsParallel().FirstOrDefault(r => r.SearchTerm == id, null);
                }

                if (foundEnc != null && foundEnc.SearchTerm == id && foundEnc.Bitrate == bitrate)
                {
                    foundEnc.Expire = DateTime.UtcNow.AddMinutes(AudioCacheTimeout).Ticks;
                    while (foundEnc.Data == null && !foundEnc.Encoded) await Task.Delay(16);
                    if (foundEnc.Data == null) return BadRequest();
                    await Response.Body.WriteAsync(foundEnc.Data);
                    if (Bot.DebugMode)
                        await Debug.WriteAsync(
                            $"Retuning already encoded audio for search term \"{foundEnc.SearchTerm}\".");
                    await Response.CompleteAsync();
                    return null;
                }

                var stream = useOpus switch
                {
                    true => await ff.Convert(first, codec: "-c:a libopus", addParameters: $"-b:a {bitrate}k"),
                    false => await ff.Convert(first, codec: "-c:a libvorbis", addParameters: $"-b:a {bitrate}k")
                };
                var el = new EncodedAudio
                {
                    SearchTerm = id,
                    Data = null,
                    Expire = DateTime.UtcNow.AddMinutes(AudioCacheTimeout).Ticks,
                    Encoded = false
                };
                lock (EncodedAudio)
                {
                    EncodedAudio.Add(el);
                }

                Memory<byte> memory = new byte[512];
                var ms = new MemoryStream();
                while (!stream.CanRead)
                {
                    await Task.Delay(16);
                    if (Bot.DebugMode) await Debug.WriteAsync("Waiting for readable stream.");
                }

                while (await stream.ReadAsync(memory) != 0)
                {
                    await ms.WriteAsync(memory);
                    await Response.Body.WriteAsync(memory);
                }

                el.Data = ms.ToArray();
                el.Encoded = true;
                await Response.CompleteAsync();
                return null;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"DownloadTrack failed with exception: \"{e}\"");
                throw;
            }
        }

        public async Task<IActionResult> Search(string term)
        {
            var items = await DiscordBot.Audio.Platforms.Search.Get(term, returnAllResults: true);
            var list = new List<SearchResult>();
            foreach (var item in items)
            {
                if (item is not SpotifyTrack track)
                {
                    list.Add(item.ToSearchResult());
                    continue;
                }
                var spotify = await Video.Search(track);
                var res = spotify.ToSearchResult();
                res.IsSpotify = false;
                list.Add(res);
            }
            return Json(list, new JsonSerializerOptions {PropertyNameCaseInsensitive = false});
        }

        public FileStreamResult GetRandomDownload()
        {
            var files = Directory.EnumerateFiles($"{Bot.WorkingDirectory}/dll/audio").ToList();
            var rng = new Random();
            var rnFile = rng.Next(files.Count);
            FfMpeg2 ff = new();
            return File(ff.Convert(files[rnFile]), "audio/ogg", $"{files[rnFile][..^5].Split("/").Last()}.ogg");
        }

        public IActionResult GetNewSocketSession()
        {
            var newSession = new SocketSession
            {
                Id = Guid.NewGuid(),
                StartExpire = DateTime.UtcNow.AddMinutes(5).Ticks
            };
            lock (GeneratedSocketSessions)
            {
                while (GeneratedSocketSessions.Any(r => r.Id == newSession.Id))
                {
                    newSession.Id = Guid.NewGuid();
                }
                GeneratedSocketSessions.Add(newSession);
            }

            return Ok(newSession.Id.ToString());
        }
    }

    public class EncodedAudio
    {
        public string SearchTerm { get; init; }
        public int Bitrate { get; set; }
        public byte[] Data { get; set; }
        public long Expire { get; set; }
        public bool Encoded { get; set; }
    }

    public struct SocketSession
    {
        public Guid Id;
        public long StartExpire;
    }
}