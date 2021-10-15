using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bat_Tosho.Audio;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Bat_Tosho.Messages
{
    public class Statusbar
    {
        private const char EmptyBlock = '□'; //Old: '□';
        private const char FullBlock = '■'; //Old: '■';
        private static int _pl0, _pl1 = 1, _pl2 = 2, _pl3 = 3, _pl4 = 4;
        private bool _stopped;
        public long Current, Max;
        public StatusbarStatus Status = StatusbarStatus.Null;
        private Stopwatch WaitingStopwatch { get; }= new();
        public bool UpdatePlacement { get; set; }
        private static string SongName => "";
        public static string Message => "";

        public async Task Update(CommandContext ctx, Instance instance)
        {
            _stopped = false;
            while (!_stopped)
            {
                if (instance.StatusbarMessage != null)
                    switch (Status)
                    {
                        case StatusbarStatus.AddingVideos:
                            await instance.StatusbarMessage.ModifyAsync(ReturnGenericStatus("Adding Videos", SongName,
                                Current, Max));
                            break;

                        case StatusbarStatus.Playing:
                            if (WaitingStopwatch.IsRunning)
                                WaitingStopwatch.Reset();
                            await UpdatePlaying(ctx, instance);
                            break;

                        case StatusbarStatus.Message:
                            await instance.StatusbarMessage.ModifyAsync(Message);
                            break;

                        case StatusbarStatus.Null:
                            break;
                        case StatusbarStatus.Waiting:
                            if (!WaitingStopwatch.IsRunning)
                                WaitingStopwatch.Start();
                            try
                            {
                                await instance.StatusbarMessage.ModifyAsync(ReturnGenericStatus("Waiting", "For 15 minutes and then leaving", WaitingStopwatch.ElapsedMilliseconds, 900000, true));
                                await StatusbarButtons(instance, ctx);
                            }
                            catch (Exception e)
                            {
                                await Debug.Write($"Waiting Statusbar Generation Failed: {e}");
                            }
                            break;
                        
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                await Task.Delay(1600);
            }
        }

        private async Task UpdatePlaying(CommandContext ctx, Instance instance)
        {
            var messagesAfter = await ctx.Channel.GetMessagesAfterAsync(instance.StatusbarMessage.Id);
            var showNext = true;
            VideoInformation nextSong = null;
            switch (instance.VideoInfos.Count)
            {
                case 0:
                    return;
                case 1:
                    showNext = false;
                    break;
                case >=2:
                    if (instance.Song + 1 < instance.VideoInfos.Count)
                        nextSong = instance.VideoInfos[instance.Song + 1];
                    break;
            }

            VideoInformation currentSong;
            try
            {
                currentSong = instance.VideoInfos[instance.Song];
            }
            catch (Exception)
            {
                await Task.Delay(500); //yessss
                try
                {
                    currentSong = instance.VideoInfos[instance.Song];
                }
                catch (Exception)
                {
                    currentSong = instance.VideoInfos.First();
                }
            }

            var statusbar = currentSong.LengthMs switch
            {
                -1 => ReturnStreamStatus(currentSong, currentSong.Stopwatch.ElapsedMilliseconds,
                    showNext switch {false => null, _ => nextSong}, instance.Song, instance.VideoInfos.Count, instance),
                _ => ReturnStatus(currentSong, currentSong.Stopwatch.ElapsedMilliseconds,
                    showNext switch {false => null, _ => nextSong}, instance.Song, instance.VideoInfos.Count, instance)
            };
            if (messagesAfter.Count > 2 || UpdatePlacement)
            {
                try
                {
                    await instance.StatusbarMessage.DeleteAsync();
                }
                catch (Exception e)
                {
                    await Debug.Write($"Generating Statusbar: Deleting old message failed: {e}");
                }

                try
                {
                    instance.StatusbarMessage =
                        await ctx.Client.SendMessageAsync(instance.StatusbarChannel, statusbar);
                }
                catch (Exception e)
                {
                    await Debug.Write($"Generating Statusbar: Generating new message failed: {e}");
                }
                UpdatePlacement = false;
            }
            else
            {
                try
                {
                    await instance.StatusbarMessage.ModifyAsync(statusbar);
                }
                catch (Exception e)
                {
                    await Debug.Write($"Generating Statusbar: Modifying existing message failed: {e}");
                    UpdatePlacement = true;
                }
            }

            await StatusbarButtons(instance, ctx);
        }

        private static async Task StatusbarButtons(Instance instance, CommandContext ctx)
        {
            List<DiscordEmoji> emojis = new()
            {
                DiscordEmoji.FromName(ctx.Client, ":track_previous:"),
                DiscordEmoji.FromName(ctx.Client, ":twisted_rightwards_arrows:"),
                DiscordEmoji.FromName(ctx.Client, ":play_pause:"),
                DiscordEmoji.FromName(ctx.Client, ":stop_button:"),
                DiscordEmoji.FromName(ctx.Client, ":track_next:")
            };
            var reactions = new List<IReadOnlyList<DiscordUser>>();
            try
            {
                reactions = new List<IReadOnlyList<DiscordUser>>
                {
                    await instance.StatusbarMessage.GetReactionsAsync(emojis[0]),
                    await instance.StatusbarMessage.GetReactionsAsync(emojis[1]),
                    await instance.StatusbarMessage.GetReactionsAsync(emojis[2]),
                    await instance.StatusbarMessage.GetReactionsAsync(emojis[3]),
                    await instance.StatusbarMessage.GetReactionsAsync(emojis[4])
                };
            }
            catch (Exception e)
            {
                await Debug.Write($"Failed to get reactions. Probably rate limited. {e}");
            }

            for (var i = 0; i < reactions.Count; i++)
            {
                if (reactions[i].Count == 0)
                {
                    await instance.StatusbarMessage.CreateReactionAsync(emojis[i]);
                    await Task.Delay(333);
                    continue;
                }

                foreach (var user in reactions[i])
                {
                    if (user.IsCurrent) continue;
                    try
                    {
                        await instance.StatusbarMessage.DeleteReactionAsync(emojis[i], user);
                    }
                    catch (Exception e)
                    {
                        await Debug.Write($"Failed to delete user reaction in statusbar. {e}");
                    }

                    try
                    {
                        var member = await ctx.Guild.GetMemberAsync(user.Id);
                        if (member.VoiceState.Channel != instance.VoiceChannel) continue;
                    }
                    catch (Exception e)
                    {
                        await Debug.Write($"Checking if reaction is made by a person in the voice call failed. {e}");
                    }

                    switch (i)
                    {
                        case 0:
                            await Manager.Skip(ctx, -1);
                            break;
                        case 2:
                            await Manager.Pause(ctx);
                            break;
                        case 1:
                            await Manager.Shuffle(ctx);
                            break;
                        case 3:
                            await Manager.Leave(ctx, false);
                            break;
                        case 4:
                            await Manager.Skip(ctx);
                            break;
                    }
                }
            }
        }

        private static string ReturnStatus(VideoInformation info, long currentMs, VideoInformation next,
            int currentindex, int maxindex, Instance instance)
        {
            var time = TimeSpan.FromMilliseconds(currentMs);
            var increment = info.LengthMs / 32f;
            var display = currentMs / increment;
            var remaining = 0f;
            var progress = "";
            for (float i = 0; i < display; i++)
            {
                progress += FullBlock;
                remaining++;
            }

            for (float i = 0; i < info.LengthMs / increment - remaining; i++) progress += EmptyBlock;

            return
                "```Playing" +
                $"{info.PartOf switch {PartOf.YoutubeSearch => " Youtube Video", PartOf.DiscordAttachment => " Discord Attachment", PartOf.LocalFile => " Local File", PartOf.YoutubePlaylist => " Youtube Playlist Video", PartOf.SpotifyPlaylist => " Spotfiy Playlist Track", PartOf.SpotifyTrack => " Spotify Track", PartOf.HttpFileStream => " HTTP File Stream", _ => ""}}:\n" +
                $"({currentindex + 1} - {maxindex}) {info.Name}{string.IsNullOrEmpty(info.Author) switch {true => "", _ => $" - {info.Author}"}}\n" +
                $"{progress} ( {info.Paused switch {false => "▶️", true => "⏸️"}} {Time(time)} - {Time(info.Length)} )" +
                $"{instance.TransmitSink switch {null => "", _ => instance.TransmitSink.VolumeModifier switch {0 => " (🔇", >0 and <.33 => " (🔈", >=.33 and <=.66 => " (🔉", >.66 => " (🔊", _ => " (🔊"} + $" {(int) (instance.TransmitSink.VolumeModifier * 100)}%)"}}" +
                $"{instance.LoopStatus switch {LoopStatus.LoopOne => " ( 🔂 )", LoopStatus.LoopPlaylist => " ( 🔁 )", _ => ""}}" +
                $"{info.Requester switch {null => "", not null => $"\nRequested by: {info.Requester.Username} #{info.Requester.Discriminator}"}}" +
                $"{next switch {null => "", _ => $"\n\nNext: ({currentindex + 2}) {next.Name}{string.IsNullOrEmpty(next.Author) switch {true => "", _ => $" - {next.Author}"}}"}}```";
        }

        private static string ReturnStreamStatus(VideoInformation info, long currentMs, VideoInformation next,
            int currentindex, int maxindex, Instance instance)
        {
            var time = TimeSpan.FromMilliseconds(currentMs);
            var progress = new char[32];
            for (var i = 0; i <= progress.GetUpperBound(0); i++) progress[i] = EmptyBlock;

            progress[_pl0] = progress[_pl1] = progress[_pl2] = progress[_pl3] = progress[_pl4] = FullBlock;
            for (var i = 0; i < 2; i++)
            {
                if (++_pl0 > 31)
                    _pl0 = 0;
                if (++_pl1 > 31)
                    _pl1 = 0;
                if (++_pl2 > 31)
                    _pl2 = 0;
                if (++_pl3 > 31)
                    _pl3 = 0;
                if (++_pl4 > 31)
                    _pl4 = 0;
            }

            var pr = progress.Aggregate("", (current, ch) => current + ch);
            return "```Playing" +
                   $"{info.PartOf switch {PartOf.YoutubeSearch => " Youtube Video", PartOf.DiscordAttachment => " Discord Attachment", PartOf.LocalFile => " Local File", PartOf.YoutubePlaylist => " Youtube Playlist Video", PartOf.SpotifyPlaylist => " Spotfiy Playlist Track", PartOf.SpotifyTrack => " Spotify Track", PartOf.HttpFileStream => " HTTP File Stream", _ => ""}}:\n" +
                   $"({currentindex + 1} - {maxindex}) {info.Name} - {info.Author}\n" +
                   $"{pr} ( {info.Paused switch {false => "▶️", true => "⏸️"}} {Time(time)} - ∞ )" +
                   $"{instance.TransmitSink switch {null => "", _ => instance.TransmitSink.VolumeModifier switch {0 => " (🔇", >0 and <33 => " (🔈", >=33 and <=66 => "🔉", >66 => " (🔊", _ => "🔊"} + $" {instance.TransmitSink.VolumeModifier}%)"}}" +
                   $"{next switch {null => "", _ => $"\n \nNext: ({currentindex + 2}) {next.Name} - {next.Author}"}}```";
        }

        private static string ReturnGenericStatus(string status, string message, long current, long max = -1, bool timer = false)
        {
            
            var increment = max / 32f;
            var display = current / increment;
            var remaining = 0f;
            var progress = "";
            for (float i = 0; i < display; i++)
            {
                progress += FullBlock;
                remaining++;
            }

            for (float i = 0; i < max / increment - remaining; i++) progress += EmptyBlock;

            if (!timer) 
                return $"```{status}:\n" +
                               $"{message}\n" +
                               $"{progress} ({current}{max switch {<0 => "", _ => $" - {max}"}})```";
            
            return $"```{status}:\n" +
                   $"{message}\n" +
                   $"{progress} ({TimeSpan.FromMilliseconds(current):mm\\:ss} - {TimeSpan.FromMilliseconds(max):mm\\:ss})```";
        }

        private static string Time(TimeSpan span)
        {
            return span.ToString("hh\\:mm\\:ss");
        }

        public async Task Stop()
        {
            _stopped = true;
            await Debug.Write("Stopping Statusbar.");
        }

        public async Task Stop(Instance instance, string message, bool formated = true)
        {
            _stopped = true;
            await Debug.Write("Stopping Statusbar and Sending Message.");
            await instance.StatusbarMessage.ModifyAsync(formated switch {true => $"```{message}```", false => message});
        }
    }
}