using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Readers;
using DiscordBot.Tools;
using YouTubeApiSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Audio.Objects
{
    public class YoutubeVideoInformation : PlayableItem
    {
        private const string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/audio";
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
            var updateTask = Task.Run(async () => { await DownloadYtDlp(YoutubeId, true); });
            updateTask.Wait();
            return Location;
        }

        //This is a bad way to implement this feature, but I cannot currently implement it in a better way... Well, too bad!
        public override async Task<bool> GetAudioData(params Stream[] outputs)
        {
            TriesToDownload++;
            Errored = TriesToDownload > 3;
            try
            {
                Location = ReturnIfExists(YoutubeId);
                if (!string.IsNullOrEmpty(Location))
                {
                    var fs = File.Open(Location, FileMode.Open, FileAccess.Read, FileShare.Read);
                    foreach (var stream in outputs)
                    {
                        fs.Position = 0;
                        try
                        {
                            await fs.CopyToAsync(stream);
                        }
                        catch
                        {
                            // Ignored because Streams are handled worse than my code.
                        }
                    }
                    fs.Close();
                    return true;
                }

                try
                {
                    await DownloadYtDlp(YoutubeId, false, outputs);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Download yt-dlp failed: \"{e}\"");
                    try
                    {
                        await DownloadExplode(YoutubeId, outputs);
                    }
                    catch (Exception exc)
                    {
                        await Debug.WriteAsync($"Download Youtube Explode failed: \"{exc}\"");
                        try
                        {
                            await DownloadOtherApi(YoutubeId, outputs);
                        }
                        catch (Exception except)
                        {
                            await Debug.WriteAsync($"Download Other Api failed for: \'{YoutubeId}\', with error \"{except}\"",
                                true);
                            return false;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                await Debug.WriteAsync($"Failed to download. {YoutubeId}, Exception: \"{exception}\"");
                return false;
            }

            return true;
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

        private async Task DownloadYtDlp(string id, bool live = false, params Stream[] outputs)
        {
            var outs = new List<Stream>(outputs);
            var sett = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                Arguments =
                    $"-q --no-warnings -u None -p None -r 4.0M {live switch {true => "", false => "-f bestaudio "}}--cookies \"{HttpClient.CookieDestinations.GetRandom()}\" '{id}' --output -",
                FileName = "yt-dlp"
            };
            var pr = Process.Start(sett);
            if (pr == null) throw new NullReferenceException();

            if (live)
            {
                IsLiveStream = true;
                return;
            }
            
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            var streamSpreader = new StreamSpreader(CancellationToken.None, outs.ToArray());
            await pr.StandardOutput.BaseStream.CopyToAsync(streamSpreader);
        }

        private async Task DownloadOtherApi(string id, params Stream[] outputs)
        {
            var outs = new List<Stream>(outputs);
            var videoInfos =
                DownloadUrlResolver.GetDownloadUrls($"https://youtube.com/watch?v={id}");
            if (videoInfos == null) throw new Exception("Empty Results");
            var results = videoInfos.ToList();
            var audioInfo = results.Where(vi => vi.Resolution == 0 && vi.AudioType == AudioType.Opus)
                .OrderBy(vi => vi.AudioBitrate).Last();
            if (audioInfo.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(audioInfo);
            Location = audioInfo.DownloadUrl;
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(audioInfo.DownloadUrl), false,
                outs.ToArray());
        }

        private async Task DownloadExplode(string id, params Stream[] outputs)
        {
            var outs = new List<Stream>(outputs);
            var client = new YoutubeClient(HttpClient.WithCookies());
            var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var filepath = $"{DownloadDirectory}/{id}.{streamInfo.Container}";
            Location = streamInfo.Url;
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(filepath), false,
                outs.ToArray());
        }
    }
}