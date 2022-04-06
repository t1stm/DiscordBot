using System.Diagnostics;
using System.IO;

namespace BatToshoRESTApp.Standalone
{
    public class FfMpeg2
    {
        private Process FfMpegProcess { get; set; }

        public Stream Convert(string path, string format = "-f ogg", string codec = "-c:a copy",
            string addParameters = " ")
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = @"-v quiet -nostats " +
                            $@"-i ""{path}"" {codec} {addParameters} {format} pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardInput = false,
                UseShellExecute = false
            };
            FfMpegProcess = Process.Start(ffmpegStartInfo);
            return FfMpegProcess?.StandardOutput.BaseStream;
        }
    }
}