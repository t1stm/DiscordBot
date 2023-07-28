using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Readers;
using Result;
using Result.Objects;
using Streams;
using YouTubeApiSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordBot.Audio.Objects;

public class YoutubeVideoInformation : PlayableItem
{
    private static readonly string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/audio";
    private bool IsLiveStream { get; set; }
    public string SearchTerm { get; init; }
    public string YoutubeId { get; set; }
    public string ThumbnailUrl { get; init; } = "";
    private int TriesToDownload { get; set; }

    public override string GetThumbnailUrl()
    {
        return ThumbnailUrl;
    }

    public override string GetAddUrl()
    {
        return $"yt://{YoutubeId}";
    }

    public bool GetIfLiveStream()
    {
        return IsLiveStream;
    }

    public override string GetLocation()
    {
        if (!IsLiveStream) return Location;
        var updateTask = Task.Run(() =>
        {
            DownloadYtDlp(YoutubeId, true);
            return Task.CompletedTask;
        });
        updateTask.Wait();
        return Location;
    }

    //This is a bad way to implement this feature, but I cannot currently implement it in a better way... Well, too bad!
    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        var outs = new List<Stream>(outputs);
        TriesToDownload++;
        Errored = TriesToDownload > 3;

        if (Length < 1800000 && YoutubeId != "ETQmQ1Ixv5Y")
            outs.Add(File.Open($"{DownloadDirectory}/{YoutubeId}.webm", FileMode.Create));

        Location = ReturnIfExists(YoutubeId);
        if (!string.IsNullOrEmpty(Location))
        {
            var stream_spreader = new StreamSpreader(outs.ToArray())
            {
                IsAsynchronous = true,
                KeepCached = true
            };

            await using var fs = File.Open(Location, FileMode.Open, FileAccess.Read, FileShare.Read);
            await fs.CopyToAsync(stream_spreader).ContinueWith(_ => { stream_spreader.FinishWriting(); })
                .ConfigureAwait(false);

            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }

        var yt_dlp = DownloadYtDlp(YoutubeId);
        if (yt_dlp == Status.OK)
        {
            var (stream, task) = yt_dlp.GetOK();

            var stream_spreader = new StreamSpreader(outs.ToArray())
            {
                IsAsynchronous = true,
                KeepCached = true
            };

            await stream.CopyToAsync(stream_spreader).ContinueWith(_ => { stream_spreader.FinishWriting(); })
                .ConfigureAwait(false);
            await task;

            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }

        var yt_other = await DownloadOtherApi(YoutubeId);
        if (yt_other == Status.OK)
        {
            var stream_spreader = yt_other.GetOK();
            stream_spreader.AddDestinations(outs.ToArray());

            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }

        var yt_explode = await DownloadExplode(YoutubeId);
        if (yt_explode != Status.OK) return Result<StreamSpreader, Error>.Error(new UnknownError());

        {
            var stream_spreader = yt_explode.GetOK();
            stream_spreader.AddDestinations(outs.ToArray());

            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }
    }

    public override string GetId()
    {
        return YoutubeId ?? "";
    }

    private static string ReturnIfExists(string id)
    {
        if (File.Exists($"{DownloadDirectory}/{id}.webm") &&
            new FileInfo($"{DownloadDirectory}/{id}.webm").Length > 0)
            return $"{DownloadDirectory}/{id}.webm";
        if (File.Exists($"{DownloadDirectory}/{id}.mp4") &&
            new FileInfo($"{DownloadDirectory}/{id}.mp4").Length > 0)
            return $"{DownloadDirectory}/{id}.mp4";
        return File.Exists($"{DownloadDirectory}/{id}.mp3") &&
               new FileInfo($"{DownloadDirectory}/{id}.mp3").Length > 0
            ? $"{DownloadDirectory}/{id}.mp3"
            : null;
    }

    private Result<(Stream, Task), Error> DownloadYtDlp(string id, bool live = false)
    {
        var sett = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            Arguments =
                $"-q --no-warnings -u None -p None -r 4.0M {live switch { true => "", false => "-f bestaudio " }}" +
                $"--cookies \"{HttpClient.CookieDestinations.GetRandom()}\" \"https://youtube.com/watch?v={id}\" --output -",
            FileName = "yt-dlp"
        };
        var pr = Process.Start(sett);
        if (pr == null) return Result<(Stream, Task), Error>.Error(new NoResultsError());

        if (!live) return Result<(Stream, Task), Error>.Success((pr.StandardOutput.BaseStream, pr.WaitForExitAsync()));

        IsLiveStream = true;
        return Result<(Stream, Task), Error>.Error(new NoResultsError());
    }

    private async Task<Result<StreamSpreader, Error>> DownloadOtherApi(string id)
    {
        var videoInfos =
            DownloadUrlResolver.GetDownloadUrls($"https://youtube.com/watch?v={id}");
        if (videoInfos == null) return Result<StreamSpreader, Error>.Error(new NoResultsError());

        var results = videoInfos.ToList();
        var audioInfo = results.Where(vi => vi.Resolution == 0 && vi.AudioType == AudioType.Opus)
            .OrderBy(vi => vi.AudioBitrate).Last();
        if (audioInfo.RequiresDecryption)
            DownloadUrlResolver.DecryptDownloadUrl(audioInfo);
        Location = audioInfo.DownloadUrl;

        return await HttpClient.ChunkedDownloader(HttpClient.WithCookies(), new Uri(audioInfo.DownloadUrl), true)
            .ConfigureAwait(false);
    }

    private async Task<Result<StreamSpreader, Error>> DownloadExplode(string id)
    {
        var client = new YoutubeClient(HttpClient.WithCookies());
        var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        var filepath = $"{DownloadDirectory}/{id}.{streamInfo.Container}";
        Location = streamInfo.Url;

        return await HttpClient.ChunkedDownloader(HttpClient.WithCookies(), new Uri(filepath), true)
            .ConfigureAwait(false);
    }
}