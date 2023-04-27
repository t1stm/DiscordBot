using System.Diagnostics;
using System.Text;
using DiscordBot;
using DiscordBot.Abstract;
using DiscordBot.Audio.Platforms;
using DiscordBot.Audio.Platforms.Youtube;
using Result.Objects;
using Debug = DiscordBot.Methods.Debug;

namespace TestingApp.Storage_Cleanup_Tests;

public static class RemoveWrongFiles
{
    private const string working_directory = $"{Bot.WorkingDirectory}/dll/audio";
    public static async Task Run()
    {
        var read = Directory.EnumerateFiles(working_directory);
        var invalid_locations = new List<string>();

        async void Check(string file)
        {
            var is_valid = await IsMatchingLength(file);
            if (is_valid) return;

            invalid_locations.Add(file);
            await Debug.WriteAsync($"Invalid file: \'{file}\'");
        }

        foreach (var s in read)
        {
            Check(s);
        }
        
        var string_builder = new StringBuilder();
        foreach (var location in invalid_locations)
        {
            string_builder.Append($"\'{location}\', ");
        }

        await Debug.WriteAsync($"Invalid files: {string_builder}");
    }

    private static async Task<bool> IsMatchingLength(string location)
    {
        try
        {
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $@"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 {location}",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = false,
                UseShellExecute = false
            };
            var ffmpeg = Process.Start(ffmpegStartInfo);
            if (ffmpeg == null) return false;
        
            var video_id = location.Split('/').Last().Split('.').First();
            var search = Video.GetCachedVideoFromId(video_id);
            PlayableItem video;
            if (search == Status.Error)
            {
                var search_online = await Search.Get($"yt://{video_id}");
                var first = search_online.GetOK().First();
                video = first;
            }
            else
            {
                video = search.GetOK();
                await Debug.WriteAsync($"Found cached video: \'{video_id}\'");
            }

            var video_length = video.GetLength();
            var ffmpeg_output = await ffmpeg.StandardOutput.ReadToEndAsync();

            var ffmpeg_duration = Math.Round(double.Parse(ffmpeg_output) * 1000);

            return Math.Abs(video_length - ffmpeg_duration) < 2000;
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Exception in processing. \'{e}\'");
            return false;
        }
    }
}