using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;

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

        public async Task<Stream> Convert(PlayableItem item, string format = "-f ogg", string codec = "-c:a copy",
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
            var yes = new Task(async () => { await item.GetAudioData(FfMpegProcess.StandardInput.BaseStream); });
            yes.Start();
            return FfMpegProcess?.StandardOutput.BaseStream;
        }
    }
}