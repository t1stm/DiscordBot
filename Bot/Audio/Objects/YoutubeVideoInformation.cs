using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Readers;
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
        public override async Task GetAudioData(params Stream[] outputs)
        {
            TriesToDownload++;
            Errored = TriesToDownload > 3;
            try
            {
                Location = ReturnIfExists(YoutubeId);
                if (!string.IsNullOrEmpty(Location))
                {
                    var ms = new MemoryStream();
                    var fs = File.OpenRead(Location);
                    await fs.CopyToAsync(ms);
                    fs.Close();
                    foreach (var stream in outputs)
                    {
                        ms.Position = 0;
                        try
                        {
                            await ms.CopyToAsync(stream);
                        }
                        catch
                        {
                            // Ignored because Streams are handled worse than my code.
                        }
                    }

                    return;
                }

                try
                {
                    await DownloadYtDlp(YoutubeId, false, outputs);
                }
                catch (Exception)
                {
                    try
                    {
                        await DownloadExplode(YoutubeId, outputs);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            await DownloadOtherApi(YoutubeId, outputs);
                        }
                        catch (Exception exc)
                        {
                            await Debug.WriteAsync($"No downloadable audio for {YoutubeId}, With error {exc}",
                                true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Failed to download. {YoutubeId}, Exception: \"{e}\"");
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

        private async Task DownloadYtDlp(string id, bool live = false, params Stream[] outputs)
        {
            var outs = new List<Stream>(outputs);
            var sett = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments =
                    $"-g {live switch {true => "", false => "-f bestaudio "}}--cookies \"{HttpClient.CookieDestinations.GetRandom()}\" {id}",
                FileName = "yt-dlp"
            };
            var pr = Process.Start(sett);
            if (pr == null) throw new NullReferenceException();
            await pr.WaitForExitAsync();
            var url = await pr.StandardOutput.ReadLineAsync();
            var err = await pr.StandardError.ReadLineAsync();
            if (live)
            {
                IsLiveStream = true;
                Location = url;
                return;
            }

            if (!string.IsNullOrEmpty(err) && err.Contains("Requested format is not available"))
                await DownloadYtDlp(id, true);

            Location = url ?? throw new NullReferenceException();
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(url), false, outs.ToArray());
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

            var audioPath = $"{DownloadDirectory}/{id}{audioInfo.VideoExtension}";
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(audioPath), false,
                outs.ToArray());
            Location = audioInfo.DownloadUrl;
        }

        private async Task DownloadExplode(string id, params Stream[] outputs)
        {
            var outs = new List<Stream>(outputs);
            var client = new YoutubeClient(HttpClient.WithCookies());
            var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var filepath = $"{DownloadDirectory}/{id}.{streamInfo.Container}";
            if (Length < 1800000) outs.Add(File.Open($"{DownloadDirectory}/{id}.webm", FileMode.Create));
            await Debug.WriteAsync("Starting download task.");
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(filepath), false,
                outs.ToArray());
            YoutubeId = streamInfo.Url;
        }
    }
}