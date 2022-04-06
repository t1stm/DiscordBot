using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;
using BatToshoRESTApp.Enums;
using DSharpPlus;
using DSharpPlus.Entities;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio
{
    public class Statusbar : IBaseStatusbar
    {
        //private const string DefaultMessage = "One can use the web interface with the command: \"-webui\"";
        private const string DefaultMessage = "Мисля да преименувам главния бот на \"Слави Трифонов\" и заради това правя петиция, " +
                                              "ако искате да гласувате може да използвате /vote или -vote (slavi или tosho) и да изберете между новото и старото име. " +
                                              "Резултатите ще бъдат обявени на 14 Април 2022, на същото място като това съобщение.";
        private const char EmptyBlock = '□', FullBlock = '■';
        private int _pl0, _pl1 = 1, _pl2 = 2, _pl3 = 3, _pl4 = 4;
        private bool Stopped { get; set; }
        public Player Player { get; set; }
        public DiscordGuild Guild { get; set; }
        public DiscordChannel Channel { get; set; }
        public DiscordClient Client { get; set; }
        public DiscordMessage Message { get; set; }
        private StatusbarMode Mode { get; set; } = StatusbarMode.Stopped;
        private int UpdateDelay { get; set; } = Bot.UpdateDelay;

        public async Task UpdateStatusbar()
        {
            Player.VoiceUsers = Player?.VoiceChannel?.Users?.Count ?? 0;
            switch (Mode)
            {
                case StatusbarMode.Stopped:
                    break;
                case StatusbarMode.Playing:
                    await UpdatePlacement();
                    if (Message == null)
                        Message = await Client.Guilds[Guild.Id].Channels[Channel.Id]
                            .SendMessageAsync(GenerateStatusbar());
                    else
                        Message = await Message.ModifyAsync(GenerateStatusbar());
                    break;
                case StatusbarMode.Waiting:
                    await UpdateWaiting();
                    break;
                case StatusbarMode.Message:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task Start()
        {
            if (Message == null)
                try
                {
                    Message = await Client.Guilds[Guild.Id].Channels[Channel.Id]
                        .SendMessageAsync("```Hello! This message will update shortly.```");
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Sending Update Message Failed: {e}");
                }

            Stopped = false;
            Mode = StatusbarMode.Playing;
            while (!Stopped)
            {
                try
                {
                    if (Stopped)
                        continue; // Why do I have to add this, this doesn't make fucking sense, but it fixes a bug. Come on
                    if (Bot.DebugMode)
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        await UpdateStatusbar();
                        stopwatch.Stop();
                        await Debug.WriteAsync($"Updating statusbar took: {stopwatch.Elapsed:c}");
                    }
                    else
                    {
                        await UpdateStatusbar();
                    }
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Statusbar errored with error: {e}");
                    if (e.Message.Contains("404") || e.Message.Contains("400"))
                        Message = await Client.Guilds[Guild.Id].Channels[Channel.Id]
                            .SendMessageAsync("```Hello! This message will update shortly.```");
                }

                if (UpdateDelay > Bot.UpdateDelay) UpdateDelay -= 2000;
                if (UpdateDelay < Bot.UpdateDelay) UpdateDelay = Bot.UpdateDelay;
                await Task.Delay(UpdateDelay);
            }
        }

        public void Stop()
        {
            Stopped = true;
        }

        public void ChangeMode(StatusbarMode mode)
        {
            Mode = mode;
        }

        public async Task UpdatePlacement()
        {
            IReadOnlyList<DiscordMessage> messagesAfter = Array.Empty<DiscordMessage>();
            try
            {
                messagesAfter = await Channel.GetMessagesAfterAsync(Message.Id);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Getting Messages After Failed: {e}");
            }

            if (messagesAfter.Count > 2 && messagesAfter.Count > Bot.Clients.Count)
            {
                var guild = await Client.GetGuildAsync(Guild.Id);
                var chan = guild.Channels.First(ch => ch.Key == Channel.Id).Value;
                await chan.DeleteMessageAsync(Message);
                Message = null;
            }
        }

        public string GenerateStatusbar()
        {
            PlayableItem next;
            try
            {
                next = Player.Queue.GetNext();
            }
            catch (Exception)
            {
                next = null;
            }

            var req = Player.CurrentItem.GetRequester();
            var length = Player.CurrentItem.GetLength();
            var time = Player.Stopwatch.ElapsedMilliseconds;
            var progress = GenerateProgressbar(Player);
            var message = string.IsNullOrEmpty(Player.StatusbarMessage)
                ? $"\n\n{DefaultMessage}"
                : $"\n\n{Player.StatusbarMessage}";
            if (Bot.DebugMode)
                Debug.Write(
                    $"Updated Statusbar in guild \"{Player.CurrentGuild.Name}\": Track: \"{Player.CurrentItem.GetName()}\", Time: {Time(Player.Stopwatch.Elapsed)} - {Time(TimeSpan.FromMilliseconds(length))}");

            return
                $"```Playing {Player.CurrentItem.GetTypeOf()}:\n" +
                $"({Player.Queue.Current + 1} - {Player.Queue.Count}) {Player.CurrentItem.GetName()}\n" +
                $"{progress} ( {Player.Paused switch {false => "▶️", true => "⏸️"}} {Time(TimeSpan.FromMilliseconds(time))} - {length switch {0 => "∞", _ => Time(TimeSpan.FromMilliseconds(length))}} )" +
                $"{Player.Sink switch {null => "", _ => Player.Sink.VolumeModifier switch {0 => " (🔇", >0 and <.33 => " (🔈", >=.33 and <=.66 => " (🔉", >.66 => " (🔊", _ => " (🔊"} + $" {(int) (Player.Sink.VolumeModifier * 100)}%)"}}" +
                $"{Player.LoopStatus switch {Loop.One => " ( 🔂 )", Loop.WholeQueue => " ( 🔁 )", _ => ""}}" +
                $"{req switch {null => "", not null => $"\nRequested by: {req.Username} #{req.Discriminator}"}}" +
                $"{next switch {null => "", _ => $"\n\nNext: ({Player.Queue.Current + 2}) {next.GetName()}"}}" +
                $"{message}```";
        }

        private async Task UpdateWaiting()
        {
            await Message.ModifyAsync(
                $"```Waiting:\nFor 15 minutes and then leaving.\n{GenerateProgressbar(Player.WaitingStopwatch.ElapsedMilliseconds, 900000)} ( {Player.WaitingStopwatch.Elapsed:mm\\:ss} - 15:00 )```");
        }

        public async Task UpdateMessageAndStop(string message, bool formatted = true)
        {
            try
            {
                Stop();
                await Task.Delay(Bot.UpdateDelay);
                await Message.ModifyAsync(formatted ? $"```{message}```" : message);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Error when stopping statusbar: {e}");
            }
        }

        private string GenerateProgressbar(Player player)
        {
            var total = (long) player.Queue.GetCurrent().GetLength();
            var current = player.Stopwatch.ElapsedMilliseconds;
            return GenerateProgressbar(current, total);
        }

        private string GenerateProgressbar(long current, long total)
        {
            var progress = "";
            if (total != 0)
            {
                var time = current;
                var increment = total / 32f;
                var display = time / increment;
                var remaining = 0f;
                for (float i = 0; i < (display > 32 ? 32 : display); i++)
                {
                    progress += FullBlock;
                    remaining++;
                }

                for (float i = 0; i < total / increment - remaining; i++) progress += EmptyBlock;
            }
            else
            {
                var prg = new char[32];
                for (var i = 0; i <= prg.GetUpperBound(0); i++) prg[i] = EmptyBlock;

                prg[_pl0] = prg[_pl1] = prg[_pl2] = prg[_pl3] = prg[_pl4] = FullBlock;
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

                progress = prg.Aggregate(progress, (current1, ch) => current1 + ch);
            }

            return progress;
        }

        public static string Time(TimeSpan span)
        {
            return span.ToString("hh\\:mm\\:ss");
        }

        ~Statusbar()
        {
            Stop();
        }
    }
}