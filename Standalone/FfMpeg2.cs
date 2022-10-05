using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Standalone
{
    public class FfMpeg2
    {
        public Process FfMpegProcess { get; set; }

        public Stream Convert(string path, string format = "-f ogg", string codec = "-c:a copy",
            string addParameters = " ")
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-v quiet -nostats " + $@"-i ""{path}"" {codec} -vn {addParameters} {format} pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            return FfMpegProcess?.StandardOutput.BaseStream;
        }

        public Stream Convert(PlayableItem item, string format = "-f ogg", string codec = "-c:a copy",
            string addParameters = " ")
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-v quiet -nostats " + $@"-i - {codec} -vn {addParameters} {format} pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
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
            return FfMpegProcess?.StandardOutput.BaseStream;
        }
    }
}