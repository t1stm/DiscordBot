using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DSharpPlus.VoiceNext;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Audio.Objects
{
    public class FfMpeg
    {
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

        public async Task ItemToPcm(PlayableItem item, VoiceTransmitSink destination,
            string startingTime = "00:00:00.000", bool normalize = true)
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
                RedirectStandardError = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            if (FfMpegProcess == null) return;
            try
            {
                var ms = new MemoryStream();
                var ended = false;
                var yes = new Task(async () =>
                {
                    await item.GetAudioData(ms);
                    ended = true;
                });
                yes.Start();
                var task = new Task(async () =>
                {
                    try
                    {
                        while (ms.Length < 10) await Task.Delay(4);
                        for (var i = 0; i < ms.Length; i++)
                        {
                            while (i == ms.Length - 2 && !ended)
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

                        FfMpegProcess.StandardInput.Close();
                    }
                    catch (Exception e)
                    {
                        if (e.Message.ToLower().Contains("broken") && e.Message.ToLower().Contains("pipe")) return;
                        await Debug.WriteAsync($"Writing byte falure: {e}");
                    }
                });
                task.Start();
                await FfMpegProcess.StandardOutput.BaseStream.CopyToAsync(destination);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Copying PlayableItem stream to FFmpeg threw exception: \"{e}\"");
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