using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio;
using DiscordBot.Enums;
using DSharpPlus;
using DSharpPlus.Entities;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Messages
{
    public class Statusbar : IBaseStatusbar
    {
        //private const string DefaultMessage = "One can use the web interface with the command: \"-webui\"";
        private const string DefaultMessage =
            "The bot is currently being reworked majorly, so please note that there may be many bugs.";

        //private const string DefaultMessage =
        //    "След пресмятането на резултатите, новото име бие с 86.5% от гласовето. На 20 Април, този бот вече ще се казва \"Слави Трифонов\". Благодаря на всички които гласуваха.";
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
            var users = Player?.VoiceChannel?.Users;
            if (Player != null) Player.VoiceUsers = (List<DiscordMember>) users;
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
            var stopwatch = new Stopwatch();
            while (!Stopped)
            {
                try
                {
                    if (Stopped)
                        continue;
                    stopwatch.Restart();
                    await UpdateStatusbar();
                    UpdateDelay += (int) stopwatch.ElapsedMilliseconds / 2;
                    if (Bot.DebugMode)
                    {
                        stopwatch.Stop();
                        await Debug.WriteAsync($"Updating statusbar took: {stopwatch.Elapsed:c} / Update Delay is: {UpdateDelay}ms");
                    }
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Statusbar errored with error: {e}");
                    if (e.Message.Contains("404") || e.Message.Contains("400"))
                        Message = await Client.Guilds[Guild.Id].Channels[Channel.Id]
                            .SendMessageAsync("```Hello! This message will update shortly.```");
                }

                if (UpdateDelay > Bot.UpdateDelay) UpdateDelay -= Bot.UpdateDelay / 3;
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
                $"{req switch {null => "", _ => $"\nRequested by: {req.Username} #{req.Discriminator}"}}" +
                $"{next switch {null => "", _ => $"\n\nNext: ({Player.Queue.Current + 2}) {next.GetName()}"}}" +
                $"{message}```";
        }

        private async Task UpdateWaiting()
        {
            await Message.ModifyAsync(
                $"```Waiting:\nFor 15 minutes and then leaving.\n{GenerateProgressbar(Player.WaitingStopwatch.ElapsedMilliseconds, 900000)} ( {Player.WaitingStopwatch.Elapsed:mm\\:ss} - 15:00 )\n\n{DefaultMessage}```");
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

        private string GenerateProgressbar(long current, long total, int length = 32)
        {
            Span<char> prg = stackalloc char[length];
            if (total != 0)
            {
                var increment = total / length;
                var display = (int) (current / increment);
                display = display > length ? length : display;
                for (var i = 0; i < display; i++) prg[i] = FullBlock;

                for (var i = display; i < length; i++) prg[i] = EmptyBlock;

                return prg.ToString();
            }

            length = length < 4 ? 32 : length;
            prg = stackalloc char[length];
            for (var i = 0; i < length; i++) prg[i] = EmptyBlock;
            for (var i = 0; i < 2; i++)
            {
                _pl0 = _pl0 > length - 2 ? 0 : _pl0 + 1;
                _pl1 = _pl1 > length - 2 ? 0 : _pl1 + 1;
                _pl2 = _pl2 > length - 2 ? 0 : _pl2 + 1;
                _pl3 = _pl3 > length - 2 ? 0 : _pl3 + 1;
                _pl4 = _pl4 > length - 2 ? 0 : _pl4 + 1;
            }

            prg[_pl0] = prg[_pl1] = prg[_pl2] = prg[_pl3] = prg[_pl4] = FullBlock;

            return prg.ToString();
        }

        public static string Time(TimeSpan timeSpan)
        {
            return timeSpan.ToString("hh\\:mm\\:ss");
        }

        ~Statusbar()
        {
            Stop();
        }
    }
}