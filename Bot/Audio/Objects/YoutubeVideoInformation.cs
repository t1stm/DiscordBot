using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Readers;
using DSharpPlus.Entities;
using YouTubeApiSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio.Objects
{
    public class YoutubeVideoInformation : IPlayableItem
    {
        private static readonly string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/audio";
        private bool Downloading { get; set; }
        private bool Errored { get; set; }
        public string SearchTerm { get; init; }
        public bool IsId { get; init; }
        public string YoutubeId { get; set; }
        public SpotifyTrack OriginTrack { get; set; }
        public string Location { get; private set; }
        public string Title { get; init; }
        public string Author { get; init; }
        public string ThumbnailUrl { get; init; }
        private int TriesToDownload { get; set; }
        public ulong Length { get; init; }
        public DiscordMember Requester { get; set; }

        public bool GetIfErrored()
        {
            return Errored;
        }

        public string GetTitle()
        {
            return Title;
        }

        public string GetAuthor()
        {
            return Author;
        }

        public string GetThumbnailUrl()
        {
            return ThumbnailUrl;
        }

        public string GetLocation()
        {
            return Location;
        }

        //This is a bad way to implement this feature, but I cannot currently implement it in a better way... Well, too bad!
        public async Task Download()
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

        public string GetName()
        {
            return $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";
        }

        public ulong GetLength()
        {
            return Length;
        }

        public void SetRequester(DiscordMember member)
        {
            Requester = member;
        }

        public DiscordMember GetRequester()
        {
            return Requester;
        }

        public string GetId()
        {
            return YoutubeId;
        }

        public string GetTypeOf()
        {
            return "Youtube Video";
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

        private async Task DownloadYtDlp(string id)
        {
            var sett = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = $"-g -f bestaudio --cookies \"{HttpClient.CookieDestination}\" {id}",
                FileName = "yt-dlp"
            };
            var pr = Process.Start(sett);
            if (pr == null) throw new NullReferenceException();
            await pr.WaitForExitAsync();
            var url = await pr.StandardOutput.ReadLineAsync();
            if (url == null) throw new NullReferenceException();
            var dll = new Task(async () =>
            {
                try
                {
                    if (File.Exists($"{DownloadDirectory}/{id}.webm"))
                        File.Delete($"{DownloadDirectory}/{id}.webm");
                    //await webClient.DownloadFileTaskAsync(url, $"{DownloadDirectory}/{id}.webm"); // Updating to HttpClient down below
                    //Update 30 Dec 2021: Moved the HTTP Client Downloader to it's own class.
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