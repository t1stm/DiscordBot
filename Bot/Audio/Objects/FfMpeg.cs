using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BatToshoRESTApp.Readers;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio.Objects
{
    public class FfMpeg
    {
        private bool _finishedDownloading = true;

        private readonly int Random = new Random().Next(0, int.MaxValue / 32);
        private Process FfMpegProcess { get; set; }

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
                RedirectStandardError = true,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            var task = new Task(async () =>
            {
                try
                {
                    var ms = new MemoryStream();
                    var downloadAsync = new Task(async () => { await CreateDownloadAsync(new Uri(url), ms); });
                    downloadAsync.Start();
                    var copyTask = new Task(async () =>
                    {
                        while (ms.Length < 20) await Task.Delay(3);
                        for (var i = 0; i < ms.Length; i++)
                        {
                            while (i == ms.Length - 1 && !_finishedDownloading)
                                await Task.Delay(3); // These are the spaghetti code fixes I adore.
                            var by = ms.GetBuffer()[i]; // I am starting to think that this method is quite slow.
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
                            if (e.Message.ToLower().Contains("pipe is broken")) return;
                            await Debug.WriteAsync($"FFmpeg closing stdin failed: {e}");
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

        private async Task CreateDownloadAsync(Uri uri, params Stream[] streams)
        {
            try
            {
                await Debug.WriteAsync($"Caching of \'{Random}\' starting.");
                _finishedDownloading = false;
                await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), uri, streams);
                _finishedDownloading = true;
                await Debug.WriteAsync($"Caching of \'{Random}\' completed.");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Creating Download Failed: {e}");
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