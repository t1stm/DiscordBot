using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Audio.Platforms.Youtube;
using Bat_Tosho.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;

namespace Bat_Tosho.Audio
{
    public class Player
    {
        public async Task PlayerInstance(CommandContext ctx, VoiceTransmitSink transmit)
        {
            var instance = Manager.Instances[ctx.Guild];
            for (instance.Song = 0; instance.Song < instance.VideoInfos.Count; instance.Song++)
            {
                if (instance.Song < 0) instance.Song = 0;
                while (instance.CurrentVideoInfo().Paused) await Task.Delay(100);
                await CheckIfUpdatedSpotify(instance);
                await DownloadIfNotDownloaded(instance, -255, true);
                var downloadTask = new Task(async () => { await DownloadRemaining(instance); });
                downloadTask.Start();
                instance.CurrentVideoInfo().Stopwatch.Start();
                instance.Ffmpeg = new Ffmpeg(instance.CurrentVideoInfo().Location,
                    instance.CurrentVideoInfo().Stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"));
                await instance.Ffmpeg.ConvertAudioToPcm().CopyToAsync(transmit);
                await transmit.FlushAsync();

                await instance.Ffmpeg.Kill(true, false);

                try
                {
                    if (!instance.CurrentVideoInfo().Paused)
                        instance.CurrentVideoInfo().Stopwatch
                            .Reset();
                }
                catch (Exception e)
                {
                    await Debug.Write($"Failed to reset Stopwatch. {e}");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                if (instance.Song == instance.VideoInfos.Count - 1 && instance.LoopStatus == LoopStatus.LoopPlaylist)
                {
                    instance.Song = -1;
                    instance.Statusbar.Status = StatusbarStatus.Playing;
                    instance.WaitingToLeave = false;
                    stopwatch.Reset();
                    instance.CurrentVideoInfo().Stopwatch.Reset();
                    continue;
                }

                if (instance.LoopStatus == LoopStatus.LoopOne)
                {
                    instance.Song -= 1;
                    instance.Statusbar.Status = StatusbarStatus.Playing;
                    instance.WaitingToLeave = false;
                    stopwatch.Reset();
                    instance.CurrentVideoInfo().Stopwatch.Reset();
                    continue;
                }
                while (instance.Song +1 ==  instance.VideoInfos.Count && stopwatch.Elapsed.Minutes < 15)
                {
                    await Task.Delay(1000);
                    instance.Statusbar.Status = StatusbarStatus.Waiting;
                    instance.WaitingToLeave = true;
                }
                instance.Statusbar.Status = StatusbarStatus.Playing;
                instance.WaitingToLeave = false;
                stopwatch.Reset();
                instance.CurrentVideoInfo().Stopwatch.Reset();
            }
        }

        private static async Task CheckIfUpdatedSpotify(Instance instance, int index = -255)
        {
            try
            {
                if (index == -255) index = instance.Song;
                var info = instance.VideoInfos[index];
                if (info.PartOf != PartOf.SpotifyPlaylist || info.Type != VideoSearchTypes.SearchTerm) return;
                var search = new SearchResult(info.Requester);
                var result = await search.Get($"{info.Name} - {info.Author} - Topic", info.Type, info.PartOf);
                instance.VideoInfos[index] = result.First();
            }
            catch (Exception e)
            {
                await Debug.Write($"CheckIfUpdateSpotify failed: {e}");
            }
        }

        private static async Task DownloadIfNotDownloaded(Instance instance, int index = -255, bool urgent = false)
        {
            try
            {
                if (index == -255) index = instance.Song;
                var info = instance.VideoInfos[index];
                if (info.Type is VideoSearchTypes.Downloaded or VideoSearchTypes.HttpFileStream) return;
                var down = new Download();
                if (info.Type != VideoSearchTypes.NotDownloaded)
                    throw new InvalidProgramException("Video doesn't have the NotDownloaded Tag after multiple checks.");
                await Debug.Write($"Video ID is: {info.YoutubeIdOrPathToFile}", false);
                info.Location = await down.GetFilepath(info.YoutubeIdOrPathToFile, false, urgent);
                info.Type = VideoSearchTypes.Downloaded;
            }
            catch (Exception e)
            {
                await Debug.Write($"DownloadIfNotDownloaded failed: {e}");
            }
        }

        public async Task DownloadRemaining(Instance instance)
        {
            if (instance.ActiveDownloadTasks >= 1) return;
            instance.ActiveDownloadTasks++;
            for (var i = 0; i < instance.VideoInfos.Count; i++)
            {
                try
                {
                    var info = instance.VideoInfos[i];
                    if (info.Lock || info.Type is VideoSearchTypes.Downloaded or VideoSearchTypes.HttpFileStream) continue;
                    info.Lock = true;
                    await Debug.Write($"VideoInfos.Count = {instance.VideoInfos.Count}", false);
                    await CheckIfUpdatedSpotify(instance, i);
                    await DownloadIfNotDownloaded(instance, i);
                    info.Type = VideoSearchTypes.Downloaded;
                }
                catch (Exception e)
                {
                    await Debug.Write($"Threw in for loop in Download Remaining: {e}");
                }
            }

            instance.ActiveDownloadTasks--;
        }
    }
}