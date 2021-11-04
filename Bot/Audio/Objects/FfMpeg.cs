using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio.Objects
{
    public class FfMpeg
    {
        private Process FfMpegProcess { get; set; }

        public Stream ConvertAudioToPcm(string videoPath, string startingTime = "00:00:00.000", bool normalize = true)
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-v quiet -nostats " +
                            $@"-i ""{videoPath}"" -ss {startingTime.Trim()} -c:a pcm_s16le {normalize switch {true => "-af loudnorm=I=-16:LRA=11:TP=-1.5 ", false => ""}}-ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            return FfMpegProcess?.StandardOutput.BaseStream;
        }

        public async Task Kill(bool wait = false, bool display = true)
        {
            if (wait) await Task.Delay(100);
            if (display) await Debug.WriteAsync("Killing FFMpeg");
            FfMpegProcess.Kill();
        }
    }
}