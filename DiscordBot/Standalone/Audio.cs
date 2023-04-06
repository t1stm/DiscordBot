#nullable enable
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
using Result.Objects;
using Streams;

namespace DiscordBot.Standalone;

public class Audio : Controller
{
    internal const int AudioCacheTimeout = 15;
    public static readonly List<EncodedAudio> EncodedAudio = new();
    public static readonly List<SocketSession> GeneratedSocketSessions = new();

    public static void RemoveStale(bool force = false)
    {
        var now = DateTime.UtcNow.Ticks;
        lock (EncodedAudio)
        {
            foreach (var audio in EncodedAudio.Where(r => force || r.Expire < now).ToList())
            {
                if (Bot.DebugMode)
                    Debug.Write(
                        $"Removing {(force ? "" : "stale ")}audio: \"{audio.SearchTerm}\" - Now: {now} - Expire: {audio.Expire} - Encoded: {audio.Encoded}");
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
                Debug.Write(
                    $"\"{audio.SearchTerm}\" - Now: {now} - Expire: {audio.Expire} - Remaining: {new DateTime().AddTicks(audio.Expire - now > 0 ? audio.Expire - now : 0):HH:mm:ss} - Encoded: {audio.Encoded}");
        }
    }

    [HttpGet]
    [Route("/Audio/Download/{codec}/{bitrate:int}")]
    public async Task<ActionResult> DownloadTrack(string codec, int bitrate, string id)
    {
        try
        {
            Response.StatusCode = 200;
            var type = codec switch
            {
                "Opus" or "Vorbis" => "audio/ogg",
                "AAC" => "audio/aac",
                _ => "audio/mp3"
            };
            Response.ContentType = type;
            var filename = type.Replace('/', '.'); /* type[..5] + '.' + type[7..]; */
            Response.Headers.ContentDisposition = $"attachment; filename={filename}; filename*=UTF-8''{filename}";
            Response.Headers.AcceptRanges = "none";

            if (Bot.DebugMode) await Debug.WriteAsync("Using Audio Controller");
            var res = await DiscordBot.Audio.Platforms.Search.Get(id);

            if (res != Status.OK) return BadRequest();

            var ok = res.GetOK();

            if (ok.Count < 1)
            {
                await Debug.WriteAsync("No found result in DownloadTrack API.");
                return NotFound();
            }

            var first = ok.First();

            FfMpeg2 ff = new();
            var stream = codec switch
            {
                "Opus" => ff.Convert(first, codec: "-c:a libopus", addParameters: $"-b:a {bitrate}k -d copy"),
                "Vorbis" => ff.Convert(first, codec: "-c:a libvorbis", addParameters: $"-b:a {bitrate}k -d copy"),
                "AAC" => ff.Convert(first, codec: "-c:a aac", addParameters: $"-b:a {bitrate}k -d copy",
                    format: "-f adts"),
                _ => ff.Convert(first, codec: "-c:a libmp3lame", addParameters: $"-b:a {bitrate}k -d copy",
                    format: "-f mp3")
            };

            return File(stream, type, filename, true);
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"DownloadTrack failed with exception: \"{e}\"");
            throw;
        }
    }

    //[HttpGet, Route("/Audio/UserPlaylists/{**userToken}")]
    public IActionResult GetUserPlaylists(string userToken)
    {
        return Ok(userToken);
    }

    [HttpPost]
    public IActionResult UploadPlaylist(string token, string name)
    {
        var uploadedFiles = Request.Form.Files;
        switch (uploadedFiles.Count)
        {
            case < 1:
                return NoContent();
            case > 1:
                return BadRequest();
        }

        var file = uploadedFiles[0];
        var stream = file.OpenReadStream();

        return Ok(stream);
    }

    [HttpGet]
    [Route("/Audio/Search")]
    public async Task<IActionResult> Search(string term)
    {
        var items = await DiscordBot.Audio.Platforms.Search.Get(term, returnAllResults: true);
        if (items != Status.OK) return Ok("[]"); // Empty Array
        var list = new List<SearchResult>();
        foreach (var item in items.GetOK())
        {
            if (item is not SpotifyTrack track)
            {
                list.Add(item.ToSearchResult());
                continue;
            }

            var spotify = await Video.Search(track);
            if (spotify != Status.OK) continue;
            var res = spotify.GetOK().ToSearchResult();
            res.IsSpotify = false;
            list.Add(res);
        }

        return Json(list, new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
    }

    [HttpGet]
    [Route("/Audio/GetRandomDownload")]
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
            GeneratedSocketSessions.Add(newSession);
        }

        return Ok(newSession.Id.ToString());
    }
}

public class EncodedAudio
{
    public string? SearchTerm { get; init; }
    public int Bitrate { get; set; }
    public StreamSpreader? Spreader { get; set; }
    public long Expire { get; set; }
    public bool Encoded { get; set; }
}

public struct SocketSession
{
    public Guid Id;
    public long StartExpire;
}