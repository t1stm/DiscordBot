using System;
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
        private bool Downloading { get; set; }
        private bool IsLiveStream { get; set; }
        public string SearchTerm { get; init; }
        public string YoutubeId { get; set; }
        public SpotifyTrack OriginTrack { get; set; }
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

        public new string GetLocation()
        {
            if (!IsLiveStream) return Location;
            var updateTask = Task.Run(async () => { await DownloadYtDlp(YoutubeId, true); });
            updateTask.Wait();
            return Location;
        }

        //This is a bad way to implement this feature, but I cannot currently implement it in a better way... Well, too bad!
        public override async Task Download()
        {
            TriesToDownload++;
            Errored = TriesToDownload > 3;
            if (string.IsNullOrEmpty(Location) && !Downloading && TriesToDownload < 3)
                try
                {
                    Downloading = true;
                    Location = ReturnIfExists(YoutubeId);
                    if (!string.IsNullOrEmpty(Location)) return;
                    try
                    {
                        await DownloadYtDlp(YoutubeId);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            await DownloadExplode(YoutubeId);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                DownloadOtherApi(YoutubeId);
                            }
                            catch (Exception exc)
                            {
                                await Debug.WriteAsync($"No downloadable audio for {YoutubeId}, With error {exc}",
                                    true);
                            }
                        }
                    }

                    Downloading = false;
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

        public override string GetTypeOf()
        {
            return IsLiveStream ? "Youtube Live Stream" : "Youtube Video";
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

        private async Task DownloadYtDlp(string id, bool live = false)
        {
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
            {
                await DownloadYtDlp(id, true);
                return;
            }

            if (url == null) throw new NullReferenceException();
            var dll = new Task(async () =>
            {
                try
                {
                    if (File.Exists($"{DownloadDirectory}/{id}.webm"))
                        File.Delete($"{DownloadDirectory}/{id}.webm");
                    //await webClient.DownloadFileTaskAsync(url, $"{DownloadDirectory}/{id}.webm"); // Updating to HttpClient down below
                    //Update 30 Dec 2021: Moved the HTTP Client Downloader to it's own class.
                    if (Length < 1800000)
                        Location = await HttpClient.DownloadFile(url, $"{DownloadDirectory}/{id}.webm");
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Downloading File Failed: {e}");
                }
            });
            dll.Start();
            Location = url;
        }

        private void DownloadOtherApi(string id)
        {
            var videoInfos =
                DownloadUrlResolver.GetDownloadUrls($"https://youtube.com/watch?v={id}");
            if (videoInfos == null) throw new Exception("Empty Results");
            var results = videoInfos.ToList();
            var audioInfo = results.Where(vi => vi.Resolution == 0 && vi.AudioType == AudioType.Opus)
                .OrderBy(vi => vi.AudioBitrate).Last();

            if (audioInfo.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(audioInfo);

            var audioPath = $"{DownloadDirectory}/{id}{audioInfo.VideoExtension}";
            var downTask1 = new Task(async () =>
            {
                try
                {
                    //await new WebClient().DownloadFileTaskAsync(new Uri(audioInfo.DownloadUrl), audioPath); // 30 Dec 2021: Migrated to new HttpClient
                    if (Length > 1800000) return;
                    await HttpClient.DownloadFile(audioInfo.DownloadUrl, audioPath);
                    Location = audioPath;
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Downloading File Failed: {e}");
                }
            });
            downTask1.Start();
            Location = audioInfo.DownloadUrl;
        }

        private async Task DownloadExplode(string id)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());

            var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var filepath = $"{DownloadDirectory}/{id}.{streamInfo.Container}";
            var dllTask = new Task(async () =>
            {
                try
                {
                    if (Length > 1800000) return;
                    await client.Videos.Streams.DownloadAsync(streamInfo, filepath);
                    Location = filepath;
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Download Explode, Download Task failed: {e}");
                }
            });
            dllTask.Start();
            YoutubeId = streamInfo.Url;
        }
    }
}