using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Bat_Tosho.Audio.Objects
{
    public class Ffmpeg
    {
        public readonly Process FfMpegProcess;

        public Ffmpeg(string videoPath, string startingTime = "00:00:00.000")
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-v quiet -nostats " +
                            $@"-i ""{videoPath}"" -ss {startingTime.Trim()} -c:a pcm_s16le -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
        }

        public Stream ConvertAudioToPcm()
        {
            return FfMpegProcess.StandardOutput.BaseStream;
        }

        public async Task Kill(bool wait = false, bool display = true)
        {
            if (wait) await Task.Delay(100);
            if (display) await Debug.Write("Killing FFMpeg");
            await Task.Run(() => { FfMpegProcess.Kill(); });
        }
    }
}