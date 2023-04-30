#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using Result.Objects;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Standalone;

public class FfMpeg2
{
    public Process? FfMpegProcess { get; set; }

    public Stream Convert(string path, string format = "-f ogg", string codec = "-c:a copy",
        string addParameters = " ")
    {
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = @"-v quiet -nostats " +
                        $@"-i ""{path}"" {codec} -r 48000 -vn {addParameters} {format} pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        FfMpegProcess = Process.Start(ffmpegStartInfo);
        return FfMpegProcess?.StandardOutput.BaseStream ?? Stream.Null;
    }

    public Stream Convert(PlayableItem item, string format = "-f ogg", string codec = "-c:a copy",
        string addParameters = " ")
    {
        var ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = @"-v quiet -nostats " + $@"-i - {codec} -r 48000 -vn {addParameters} {format} pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        FfMpegProcess = Process.Start(ffmpegStartInfo);
        if (FfMpegProcess == null) throw new NullReferenceException("Opened FFmpeg instance is null.");

        async void WriteAction()
        {
            try
            {
                var result = await item.GetAudioData(FfMpegProcess.StandardInput.BaseStream);
                if (result == Status.Error)
                {
                    return;
                }

                var stream_spreader = result.GetOK();
                await stream_spreader.FlushAsync();
                stream_spreader.Close();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in write action: \'{e}\'");
            }
            finally
            {
                FfMpegProcess?.StandardInput.BaseStream.DisposeAsync();
            }
        }

        var yes = new Task(WriteAction);
        yes.Start();
        return FfMpegProcess?.StandardOutput.BaseStream ?? Stream.Null;
    }
}