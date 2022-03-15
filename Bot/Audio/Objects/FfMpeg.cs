using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio.Objects
{
    public class FfMpeg
    {
        private readonly HttpClient _httpClient = new();
        private Process FfMpegProcess { get; set; }

        private bool _finishedDownloading = true;

        public Stream PathToPcm(string videoPath, string startingTime = "00:00:00.000", bool normalize = false)
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-nostats " +
                            "-v error " + //This line is going to be changed very often, I fucking know it.
                            "-hide_banner " +
                            $@"-i ""{videoPath}"" -ss {startingTime.Trim()} " +
                            "-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3554.0 Safari/537.36\" " +
                            "-reconnect_on_network_error true " +
                            "-multiple_requests true " +
                            @$"-c:a pcm_s16le {normalize switch {true => "-af loudnorm=I=-16:LRA=11:TP=-1.5 ", false => ""}}-ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            return FfMpegProcess?.StandardOutput.BaseStream;
        }

        public Stream UrlToPcm(string url, string startingTime = "00:00:00.000", bool normalize = true)
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-nostats " +
                            "-v error " + //This line is going to be changed very often, I fucking know it.
                            "-hide_banner " +
                            $@"-i - -ss {startingTime.Trim()} " +
                            "-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3554.0 Safari/537.36\" " +
                            "-reconnect_on_network_error true " +
                            "-multiple_requests true " +
                            @$"-c:a pcm_s16le {normalize switch {true => "-af loudnorm=I=-16:LRA=11:TP=-1.5 ", false => ""}}-ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            var task = new Task(async () =>
            {
                try
                {
                    var ms = new MemoryStream();
                    var downloadAsync = new Task(async () =>
                    {
                        await CreateDownloadAsync(new Uri(url), ms);
                    });
                    downloadAsync.Start();
                    var copyTask = new Task(async () =>
                    {
                        while (ms.Length < 20)
                        {
                            await Task.Delay(3);
                        }
                        for (var i = 0; i < ms.Length; i++)
                        {
                            while (i == ms.Length - 1 && !_finishedDownloading)
                            {
                                await Task.Delay(3);
                            }
                            var by = ms.GetBuffer()[i];
                            try
                            {
                                FfMpegProcess.StandardInput.BaseStream.WriteByte(by);
                            }
                            catch (Exception e)
                            {
                                if (e.Message.ToLower().Contains("broken pipe")) break;
                                await Debug.WriteAsync($"Writing byte falure: {e}");
                                break;
                            }
                        }

                        try
                        {
                            FfMpegProcess.StandardInput.Close();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                    copyTask.Start();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"FFmpeg url caching failed :( \"{e}\"");
                }
            });
            task.Start();
            return FfMpegProcess?.StandardOutput.BaseStream;
        }

        private async Task<long?> GetContentLengthAsync(string requestUri, bool ensureSuccess = true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (ensureSuccess)
                response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength;
        }

        private async Task CreateDownloadAsync(Uri uri, params Stream[] streams)
        {
            try
            {
                var fileSize = await GetContentLengthAsync(uri.AbsoluteUri) ?? 0;
                const long chunkSize = 10_485_760;
                if (fileSize == 0) throw new Exception("File has no any content");
                var segmentCount = (int) Math.Ceiling(1.0 * fileSize / chunkSize);
                _finishedDownloading = false;
                for (var i = 0; i < segmentCount; i++)
                {
                    var from = i * chunkSize;
                    var to = (i + 1) * chunkSize - 1;
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Range = new RangeHeaderValue(from, to);

                    // Download Stream
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                        response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync();
                    var buffer = new byte[81920];
                    int bytesCopied;
                    do
                    {
                        bytesCopied = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                        foreach (var output in streams)
                        {
                            await output.WriteAsync(buffer.AsMemory(0, bytesCopied));
                        }
                    } while (bytesCopied > 0 && !FfMpegProcess.HasExited);
                }
                _finishedDownloading = true;
                await Debug.WriteAsync("Downloading has finished.");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Failed: {e}");
                KillSync();
            }
        }

        public async Task Kill(bool wait = false, bool display = true)
        {
            if (wait) await Task.Delay(100);
            if (display) await Debug.WriteAsync("Killing FFMpeg");
            KillSync();
        }

        private void CancelStream()
        {
            try
            {
                FfMpegProcess.StandardOutput.DiscardBufferedData();
                FfMpegProcess.StandardOutput.BaseStream.Flush();
            }
            catch (Exception)
            {
                //Ignored
            }
        }

        public void KillSync()
        {
            CancelStream();
            try
            {
                FfMpegProcess.Kill();
            }
            catch (Exception)
            {
                //Ignored
            }
        }
    }
}