using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BatToshoRESTApp.Methods;
using YouTubeApiSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace BatToshoRESTApp.Audio.Platforms.Youtube
{
    public static class Downloader
    {
        private static readonly string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/audio";
        private static readonly HttpClient HttpClient = Readers.HttpClient.WithCookies();

        public static async Task<string> Download(string id)
        {
            if (File.Exists($"{DownloadDirectory}/{id}.webm"))
                return $"{DownloadDirectory}/{id}.webm";
            if (File.Exists($"{DownloadDirectory}/{id}.mp4"))
                return $"{DownloadDirectory}/{id}.mp4";
            try
            {
                return await DownloadExplode(id);
            }
            catch (Exception)
            {
                try
                {
                    return DownloadOtherApi(id);
                }
                catch (Exception)
                {
                    await Debug.WriteAsync($"No downloadable audio for {id}", true);
                    return null;
                }
            }
        }

        private static string DownloadOtherApi(string id)
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
                    await new WebClient().DownloadFileTaskAsync(new Uri(audioInfo.DownloadUrl), audioPath);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Downloading File Failed: {e}");
                }
            });
            downTask1.Start();
            return audioPath;
        }

        private static async Task<string> DownloadExplode(string id)
        {
            var client = new YoutubeClient(HttpClient);

            var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var address = streamInfo.Url;
            var filepath = $"{DownloadDirectory}/{id}.{streamInfo.Container}";
            var dllTask = new Task(async () =>
            {
                try
                {
                    await client.Videos.Streams.DownloadAsync(streamInfo, filepath);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Download Explode, Download Task failed: {e}");
                }
            });
            dllTask.Start();
            return address;
        }
    }
}