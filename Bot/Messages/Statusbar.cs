using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Enums;
using BatToshoRESTApp.Interfaces;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.Entities;
using TagLib.IFD.Tags;

namespace BatToshoRESTApp.Audio
{
    public class Statusbar : IBaseStatusbar
    {
        private const char EmptyBlock = '□', FullBlock = '■';
        private bool Stopped { get; set; } = false;
        public Player Player { get; set; }
        public DiscordGuild Guild { get; set; }
        public DiscordChannel Channel { get; set; }
        public DiscordClient Client { get; set; }
        private DiscordMessage Message { get; set; }
        private StatusbarMode Mode { get; set; } = StatusbarMode.Stopped;
        
        public async Task UpdateStatusbar()
        {
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

        private async Task UpdateWaiting()
        {
            await Message.ModifyAsync();
        }

        public async Task Start()
        {
            Message = await Client.Guilds[Guild.Id].Channels[Channel.Id]
                .SendMessageAsync("Hello! This message will update shortly.");
            Stopped = false;
            Mode = StatusbarMode.Playing;
            while (!Stopped)
            {
                try
                {
                    await UpdateStatusbar();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Statusbar errored with error: {e}");
                }
                await Task.Delay(Bot.UpdateDelay);
            }
        }

        public void Stop() => Stopped = true;

        public async Task UpdateMessageAndStop(string message, bool formatted = true)
        {
            Stop();
            await Task.Delay(Bot.UpdateDelay);
            await Message.ModifyAsync(formatted ? $"```{message}```" : message);
        }

        public void ChangeMode(StatusbarMode mode) => Mode = mode;

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
            if (messagesAfter.Count > 2)
            {
                var guild = await Client.GetGuildAsync(Guild.Id);
                var chan = guild.Channels.First(ch => ch.Key == Channel.Id).Value;
                await chan.DeleteMessageAsync(Message);
                Message = null;
            }
        }

        public string GenerateStatusbar()
        {
            IPlayableItem next;
            try
            {
                next = Player.Queue.GetNext();
            }
            catch (Exception)
            {
                next = null;
            }
            var req = Player.CurrentItem.GetRequester();
            var time = Player.Stopwatch.ElapsedMilliseconds;
            var increment = Player.CurrentItem.GetLength() / 32f;
            var display = time / increment;
            var remaining = 0f;
            string progress = "";
            for (float i = 0; i < display; i++)
            {
                progress += FullBlock;
                remaining++;
            }

            for (float i = 0; i < Player.CurrentItem.GetLength() / increment - remaining; i++) progress += EmptyBlock;
            
            return
                "```Playing" +
                $"{Player.CurrentItem switch {SpotifyTrack => " a Spotify Track", YoutubeVideoInformation => " a Youtube Video", SystemFile => " a Local File", _ => ""}}:\n" +
                $"({Player.Queue.Current + 1} - {Player.Queue.Count}) {Player.CurrentItem.GetName()}\n" +
                $"{progress} ( {Player.Paused switch {false => "▶️", true => "⏸️"}} {Time(TimeSpan.FromMilliseconds(time))} - {Time(TimeSpan.FromMilliseconds(Player.CurrentItem.GetLength()))} )" +
                $"{Player.Sink switch {null => "", _ => Player.Sink.VolumeModifier switch {0 => " (🔇", >0 and <.33 => " (🔈", >=.33 and <=.66 => " (🔉", >.66 => " (🔊", _ => " (🔊"} + $" {(int) (Player.Sink.VolumeModifier * 100)}%)"}}" +
                $"{Player.LoopStatus switch {Loop.One => " ( 🔂 )", Loop.WholeQueue => " ( 🔁 )", _ => ""}}" +
                $"{req switch {null => "", not null => $"\nRequested by: {req.Username} #{req.Discriminator}"}}" +
                $"{next switch {null => "", _ => $"\n\nNext: ({Player.Queue.Current + 2}) {next.GetName()}"}}```";
        }
        private static string Time(TimeSpan span)
        {
            return span.ToString("hh\\:mm\\:ss");
        }
    }
}