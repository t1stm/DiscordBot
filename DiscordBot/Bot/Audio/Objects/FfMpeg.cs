#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DSharpPlus.VoiceNext;
using Result.Objects;
using Streams;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Audio.Objects;

public class FfMpeg
{
    private Process? FfMpegProcess { get; set; }
    private StreamSpreader? Spreader { get; set; } = new(CancellationToken.None);
    private CancellationTokenSource CancellationSource { get; } = new();

    public Stream PathToPcm(string videoPath, string startingTime = "00:00:00.000", bool normalize = false)
    {
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = @"-nostats " +
                        "-v error " +
                        "-hide_banner " +
                        $@"-i ""{videoPath}"" -ss {startingTime.Trim()} " +
                        "-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3554.0 Safari/537.36\" " +
                        "-reconnect_on_network_error true " +
                        "-multiple_requests true " +
                        @$"-c:a pcm_s16le {normalize switch { true => "-af loudnorm=I=-16:LRA=11:TP=-1.5 ", false => "" }}-ac 2 -f s16le -ar 48000 pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };
        FfMpegProcess = Process.Start(ffmpegStartInfo);
        return FfMpegProcess == null ? Stream.Null : FfMpegProcess.StandardOutput.BaseStream;
    }

    public async Task ItemToPcm(PlayableItem item, VoiceTransmitSink? destination,
        string startingTime = "00:00:00.000", bool normalize = true)
    {
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = @"-nostats " +
                        "-v warning " + //This line is going to be changed very often, I fucking know it.
                        "-hide_banner " +
                        $@"-i - -ss {startingTime.Trim()} " +
                        "-user_agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3554.0 Safari/537.36\" " +
                        "-reconnect_on_network_error true " +
                        "-multiple_requests true " +
                        @$"-c:a pcm_s16le {normalize switch { true => "-af loudnorm=I=-16:LRA=11:TP=-1.5 ", false => "" }}-ac 2 -f s16le -ar 48000 pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        FfMpegProcess = Process.Start(ffmpegStartInfo);
        if (FfMpegProcess == null) return;
        
        try
        {

            async void Write()
            {
                while (!FfMpegProcess.StandardInput.BaseStream.CanWrite && !CancellationSource.Token.IsCancellationRequested) await Task.Delay(16);
                
                var result = await item.GetAudioData(FfMpegProcess.StandardInput
                    .BaseStream);
                
                if (result == Status.Error)
                {
                    await Debug.WriteAsync("Reading Audio Data wasn't successful.");
                    await Kill();
                }
                    
                var stream_spreader = result.GetOK();
                Spreader = stream_spreader;
                try
                {
                    await stream_spreader.FlushAsync(CancellationSource.Token);
                    stream_spreader.Close();
                }
                catch (TaskCanceledException)
                {
                    // Ignored
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Exception when flushing StreamSpreader: \'{e}\'");
                }
                
                await Debug.WriteAsync("Copying audio data to stream finished.");
            }

            var write_task = new Task(Write);
            write_task.Start();
            
            await FfMpegProcess.StandardOutput.BaseStream.CopyToAsync(destination);
        }
        catch (TaskCanceledException)
        {
            // Ignored
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
            CancellationSource.Cancel();
            Spreader?.Close();
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
            FfMpegProcess?.Kill();
        }
        catch (Exception)
        {
            //Ignored
        }
    }
}