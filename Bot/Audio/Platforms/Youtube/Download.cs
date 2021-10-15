using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Timers;
using YouTubeApiSharp;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Bat_Tosho.Audio.Platforms.Youtube
{
    public class Download
    {
        private const long ChunkSize = 10_485_760;
        private long _fileSize;
        private HttpClient _httpClient = new();

        public async Task<string> GetFilepath(string videoId, bool getHq = false, bool urgent = false)
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Cookie("YSC", "DIZwBK2Vq_Y", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("PREF", "tz=Europe.Sofia&f6=40000000&f5=30000", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SID",
                "BAhafqVRsKH-MfZVxpId1F0OFhNWiyOQ8aJHeKAijXNOo6xt2HmJcKWMUz8bXsU-he0nSA.", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("HSID", "AQNF3MrUdlQQYqkhe", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SSID", "At2TdZpxStA_TqDlb", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("APISID", "rxRRogLSHeUVz_AH/AAy2fW_5UxKsQ3sPC", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SAPISID", "1PbHcnU0SapJVRP-/AeTEUU6djJJF0r6ov", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("LOGIN_INFO",
                "AFmmF2swRQIgUh5saMBAsjE76LhWPhlyo0tVSwBLqcjAzN2HMYO-mP8CIQDhma5f9NCYMKsdBjryvMnoTWoXrqyB2XpgheQ_seh-hQ:QUQ3MjNmd014RXdQeUF0eHIySlpBeWZUM21UYjJuVEZPelRmdEt3R05CX1ZPbUk2RjVYZ3dPZXB3bU9MZkxfV3ZzVEh0QkFPN09GVDR1N0dra2JJcktiQ0hibElZdEszUXdGdlZPQlNZNFB1YXo5bjdteG1IdGJUS1k1eWpRWFhXSFdUQWNPaVZJRXhFSllOWU9sV3YtenBNWWhncWJDbkpNTTExTktreDRBejR3NXdSSHJ3cmNNMEFWd290Y3h3VU1FUkpmOHExT3BuaDIyUy11VXV0bnZXdkVSS2FkakIwZw==",
                "/", "youtube.com"));
            cookieContainer.Add(new Cookie("VISITOR_INFO1_LIVE", "qAx2vo_yQS8", "/", "youtube.com"));
            var handler = new HttpClientHandler {CookieContainer = cookieContainer};
            _httpClient = new HttpClient(handler);
            if (File.Exists($"{Program.MainDirectory}dll/audio/{videoId}.mp4") && !getHq)
                return $"{Program.MainDirectory}dll/audio/{videoId}.mp4";
            if (File.Exists($"{Program.MainDirectory}dll/audio/{videoId}.webm") && !getHq)
                return $"{Program.MainDirectory}dll/audio/{videoId}.webm";
            string filepath;
            try
            {
                filepath = await DownloadExplode(videoId, getHq, urgent);
            }
            catch (Exception e)
            {
                try
                {
                    await Debug.Write($"Downloading video with old api failed. {e}");
                    filepath = await DownloadNewApi(videoId, getHq);
                }
                catch (Exception exception)
                {
                    await Debug.Write($"Failed to download video with new api too. {exception}");
                    filepath = null;
                }
            }

            return filepath;
        }

        private async Task<string> DownloadNewApi(string id, bool getHq)
        {
            var videoInfos =
                DownloadUrlResolver.GetDownloadUrls($"https://youtube.com/watch?v={id}");
            if (videoInfos == null) throw new Exception("Empty Results");
            var results = videoInfos.ToList();
            var videoInfo = getHq switch
            {
                false => results.First(vi => vi.AudioType == AudioType.Unknown && vi.VideoType == VideoType.WebM),
                true => results.Last(vi => vi.AudioType != AudioType.Unknown && vi.VideoType != VideoType.Unknown)
            };
            var audioInfo = results.Where(vi => vi.Resolution == 0 && vi.AudioType == AudioType.Opus)
                .OrderBy(vi => vi.AudioBitrate).Last();

            if (audioInfo.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(audioInfo);

            var audioPath = $"{Program.MainDirectory}dll/audio/{id}{audioInfo.VideoExtension}";
            var downTask1 = new Task(async () =>
            {
                try
                {
                    await CreateDownloadAsync(new Uri(audioInfo.DownloadUrl), audioPath);
                }
                catch (Exception e)
                {
                    await Debug.Write($"Downloading high quality audio stream failed. {e}");
                    videoInfo = results.First();
                    try
                    {
                        await CreateDownloadAsync(new Uri(audioInfo.DownloadUrl), audioPath);
                    }
                    catch (Exception exception)
                    {
                        await Debug.Write($"Failed again while selecting first stream. {exception}");
                        throw;
                    }
                }
            });
            downTask1.Start();

            if (!getHq) return audioPath;
            if (videoInfo.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(videoInfo);
            var videoPath = $"{Program.MainDirectory}dll/video/{id}{videoInfo.VideoExtension}";
            var downTask2 = new Task(async () =>
            {
                await CreateDownloadAsync(new Uri(videoInfo.DownloadUrl), videoPath);
            });
            downTask2.Start();

            return audioInfo.DownloadUrl;
        }

        private async Task<string> DownloadExplode(string id, bool getHq, bool urgent)
        {
            var client = new YoutubeClient(_httpClient);

            var streamManifest = await client.Videos.Streams.GetManifestAsync(id);
            var streamInfo = getHq switch
            {
                true => streamManifest.GetMuxedStreams().GetWithHighestVideoQuality(),
                false => streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate()
            };
            string address = streamInfo.Url;
            var filepath = $"{Program.MainDirectory}dll/audio/{id}.{streamInfo.Container}";
            var dllTask = new Task(async () =>
            {
                try
                {
                    await client.Videos.Streams.DownloadAsync(streamInfo, filepath);
                    address = filepath;
                }
                catch (Exception e)
                {
                    await Debug.Write($"Download Explode, Download Task failed: {e}");
                }
            });
            dllTask.Start();
            if (!urgent)
            {
                var timer = new Stopwatch();
                timer.Start();
                while (timer.Elapsed.Seconds <= 5)
                {
                    await Task.Delay(500);
                }
            }
            else
            {
                while (dllTask.Status == TaskStatus.Running || address != filepath) await Task.Delay(500);
            }
            return address;
        }

        private async Task<long?> GetContentLengthAsync(string requestUri, bool ensureSuccess = true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (ensureSuccess)
                response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength;
        }

        public async Task CreateDownloadAsync(Uri uri, string filePath)
        {
            _fileSize = await GetContentLengthAsync(uri.AbsoluteUri) ?? 0;
            if (_fileSize == 0) throw new Exception("File has no any content! Fuck Youtube Man.");

            var timer = new Timer {Interval = 10 * 1000};
            timer.Start();
            timer.Elapsed += (_, _) =>
            {
                timer.Stop();
                File.Delete(filePath);
                throw new Exception("Took too long to download video. Switching apis.");
            };
            await using Stream output = File.OpenWrite(filePath);
            var segmentCount = (int) Math.Ceiling(1.0 * _fileSize / ChunkSize);
            for (var i = 0; i < segmentCount; i++)
            {
                var from = i * ChunkSize;
                var to = (i + 1) * ChunkSize - 1;
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Range = new RangeHeaderValue(from, to);
                using (request)
                {
                    // Download Stream
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                        response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync();
                    //File Steam
                    var buffer = new byte[81920];
                    int bytesCopied;
                    do
                    {
                        bytesCopied = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                        await output.WriteAsync(buffer.AsMemory(0, bytesCopied));
                    } while (bytesCopied > 0);

                    timer.Stop();
                }
            }
        }
    }
}