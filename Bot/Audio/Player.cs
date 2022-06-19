using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Enums;
using DiscordBot.Messages;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Debug = DiscordBot.Methods.Debug;
using Timer = System.Timers.Timer;

namespace DiscordBot.Audio
{
    public class Player
    {
        private readonly Timer _timer = new()
        {
            Interval = 1000
        };

        public Player()
        {
            Queue = new Queue();
            WebSocketManager = new WebSocketManager(Queue, this);
            Queue.Manager = WebSocketManager;
        }

        private FfMpeg FfMpeg { get; set; } = new();
        private ElapsedEventHandler Handler { get; set; }
        private bool WaitingToLeave { get; set; }
        public bool Started { get; set; }
        public WebSocketManager WebSocketManager { get; set; }
        public DiscordChannel VoiceChannel { get; set; }
        public bool Paused { get; set; }
        public bool Normalize { get; init; } = true;
        private CancellationTokenSource CancelSource { get; set; } = new();
        public Queue Queue { get; }
        public Statusbar Statusbar { get; } = new();
        public DiscordChannel Channel { get; set; }
        public DiscordClient CurrentClient { get; init; }
        public DiscordGuild CurrentGuild { get; set; }
        public VoiceTransmitSink Sink { get; set; }
        public VoiceNextConnection Connection { get; set; }
        public bool UpdatedChannel { get; private set; }
        public Loop LoopStatus { get; set; } = Loop.None;
        private bool BreakNow { get; set; }
        public PlayableItem CurrentItem { get; private set; }
        public Stopwatch Stopwatch { get; } = new();
        public Stopwatch WaitingStopwatch { get; } = new();
        public List<DiscordMember> VoiceUsers { get; set; }
        private bool Die { get; set; }
        public string StatusbarMessage { get; private set; } = "";
        private double Volume { get; set; } = 100;

        public async Task Play(int current = 0)
        {
            try
            {
                if (Die) return;
                Statusbar.Client = CurrentClient;
                Statusbar.Guild = CurrentGuild;
                Statusbar.Player = this;
                Statusbar.Channel = Channel;
                Connection.VoiceSocketErrored += async (_, args) =>
                {
                    await Debug.WriteAsync(
                        $"VoiceSocket Errored in Guild: \"{CurrentGuild.Name}\" with arguments \"{args.Exception}\"\nAttempting to reconnect.",
                        true, Debug.DebugColor.Urgent);
                    UpdateChannel(VoiceChannel);
                    await CurrentClient.SendMessageAsync(Channel,
                        "```Discord did the funny, so the bot tried to reconnect. If the playback stopped skip one time back and return to the current item.```");
                };

                var statusbar = new Task(async () => { await Statusbar.Start(); });
                statusbar.Start();
                Handler = TimerEvent;
                _timer.Elapsed += Handler;
                _timer.Start();

                for (Queue.Current = current; Queue.Current < Queue.Count; Queue.Current++)
                {
                    if (Queue.Current < 0) continue;
                    if (Die) return;
                    CurrentItem = Queue.GetCurrent();
                    Statusbar.ChangeMode(StatusbarMode.Playing);
                    while (Paused)
                    {
                        await Task.Delay(10);
                        if (!BreakNow) continue;
                        BreakNow = false;
                        break;
                    }

                    switch (CurrentItem)
                    {
                        case SpotifyTrack tr:
                            var list = await Search.Get(tr);
                            var track = list.First();
                            await track.Download();
                            Queue.Current -= 1;
                            continue;

                        case YoutubeVideoInformation vi:

                            var tries = 1;
                            while (string.IsNullOrEmpty(vi.GetLocation()) && tries < 5)
                            {
                                await vi.Download();
                                await Task.Delay(33);
                                tries++;
                            }

                            StatusbarMessage = tries > 5
                                ? $"Failed to get item: ({Queue.Current}) \"{vi.GetName()}\", skipping it."
                                : StatusbarMessage;
                            if (!vi.GetIfLiveStream())
                                await PlayTrack(vi, Stopwatch.Elapsed.ToString(@"c"), true);
                            else await PlayTrack(vi, Stopwatch.Elapsed.ToString(@"c"));
                            break;

                        default:
                            //await CurrentItem.Download();
                            await PlayTrack(CurrentItem, Stopwatch.Elapsed.ToString(@"c"));
                            break;
                    }

                    if (BreakNow)
                    {
                        BreakNow = false;
                        Stopwatch.Stop();
                        break;
                    }

                    if (!Paused && !UpdatedChannel) Stopwatch.Reset();
                    if (LoopStatus == Loop.One) Queue.Current--;
                    if (Queue.Current + 1 == Queue.Count && LoopStatus == Loop.WholeQueue) Queue.Current = -1;
                    if (Queue.Current + 1 < Queue.Count) continue;
                    WaitingToLeave = true;
                    WaitingStopwatch.Start();
                    while (WaitingToLeave)
                    {
                        Statusbar.ChangeMode(StatusbarMode.Waiting);
                        await Task.Delay(166);
                        if (Queue.Current + 1 >= Queue.Count && WaitingStopwatch.Elapsed.TotalMinutes < 15) continue;
                        WaitingToLeave = false;
                        WaitingStopwatch.Reset();
                    }
                }

                await DisconnectAsync();
                _timer.Stop();
                _timer.Elapsed -= Handler;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Player Error: {e}", true, Debug.DebugColor.Urgent);
                throw;
            }
        }

        private async void TimerEvent(object o, ElapsedEventArgs elapsedEventArgs)
        {
            await Queue.DownloadAll();
            if (WebSocketManager == null) return;
            await WebSocketManager.BroadcastCurrentTime();
        }

        private async Task PlayTrack(PlayableItem item, string startingTime, bool isYoutube = false)
        {
            try
            {
                var location = item.GetLocation();
                CancelSource = new CancellationTokenSource();
                UpdatedChannel = false;
                lock (CurrentItem) CurrentItem = item;
                FfMpeg = new FfMpeg();
                try
                {
                    await WebSocketManager.BroadcastCurrentItem();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Failed to broadcast item: \"{e}\"");
                }
                try
                {
                    await Connection.SendSpeakingAsync();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Failed to set speaking to true: \"{e}\"");
                }

                await Debug.WriteAsync($"Location is: {location}");
                if (!Stopwatch.IsRunning) Stopwatch.Start();
                UpdateVolume();
                switch (item)
                {
                    case TtsText tts:
                        await FfMpeg.TtsToPcm(tts, startingTime, Normalize)
                            .CopyToAsync(Sink, null, CancelSource.Token);
                        break;
                    default:
                        if (location is {Length: > 4} && location[..4] == "http" && isYoutube)
                            await FfMpeg.UrlToPcm(location, startingTime, Normalize)
                                .CopyToAsync(Sink, null, CancelSource.Token);
                        else
                            await FfMpeg.PathToPcm(location, startingTime, Normalize)
                                .CopyToAsync(Sink, null, CancelSource.Token);
                        break;
                }

                if (!UpdatedChannel)
                    try
                    {
                        await Connection.SendSpeakingAsync(false);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Failed to set speaking to false: {e}");
                    }
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("The operation was canceled.")) // Cancellation token
                    await Debug.WriteAsync($"PlayTrack failed: {e}", true, Debug.DebugColor.Urgent);
            }
        }

        public bool UpdateVolume(double? percent = null)
        //When will I implement this I don't know too. This has existed for over 1 year, left unused. To be honest, it has its charms.
        //EDIT 06.05.2022: Eureka! This has finally been implemented in WebSocketManager.cs. 
        {
            if (percent is > 200 or < 1) return false;
            percent ??= Volume;
            Sink.VolumeModifier = (Volume = percent.Value) / 100;
            return true;
        }

        public void UpdateChannel(DiscordChannel channel)
        {
            var task = new Task(async () =>
            {
                try
                {
                    if (CurrentGuild == null)
                    {
                        await Debug.WriteAsync("No Guild", true, Debug.DebugColor.Urgent);
                        return;
                    }

                    if (channel == null)
                    {
                        await Debug.WriteAsync($"{nameof(channel)} is null in {nameof(UpdateChannel)}.");
                        return;
                    }

                    if (VoiceChannel != null && CurrentClient != null)
                    {
                        await Debug.WriteAsync($"Current Voice Channel: {VoiceChannel?.Id} - New: {channel.Id}");
                        VoiceChannel = channel;
                        var conn = CurrentClient.GetVoiceNext().GetConnection(CurrentGuild);
                        UpdatedChannel = true;
                        conn?.Disconnect();
                    }

                    var chan = CurrentGuild.Channels[channel.Id];
                    await Task.Delay(300);
                    Connection = await CurrentClient.GetVoiceNext().ConnectAsync(chan);
                    Connection.VoiceSocketErrored += async (_, args) =>
                    {
                        await Debug.WriteAsync(
                            $"VoiceSocket Errored in Guild: \"{CurrentGuild.Name}\" with arguments \"{args.Exception}\" - Attempting to reconnect.",
                            true, Debug.DebugColor.Urgent);
                        UpdateChannel(VoiceChannel);
                        await CurrentClient.SendMessageAsync(Channel,
                            "```Discord did the funny, so the bot tried to reconnect. If the playback stopped skip one time back and return to the current item.```");
                    };
                    Sink = Connection.GetTransmitSink();
                    await Skip(0);
                    CancelSource.Cancel();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Updating Channel Failed: {e}", true, Debug.DebugColor.Urgent);
                }
            });
            task.Start();
        }

        public Loop ToggleLoop()
        {
            return LoopStatus = LoopStatus switch
            {
                Loop.None => Loop.WholeQueue, Loop.WholeQueue => Loop.One, Loop.One => Loop.None,
                _ => Loop.None
            };
        }

        public async Task Skip(int times = 1)
        {
            Paused = false;
            times -= 1;
            if (Queue.Current + times < -1) return;
            if (Queue.Current + times != Queue.Count + 1) 
                Queue.Current += times;
            await FfMpeg.Kill();
            await Sink.FlushAsync();
            CancelSource.Cancel();
        }

        public void PlsFix() // Funny Name
        {
            Queue.Current -= 1;
            FfMpeg.KillSync();
            CancelSource.Cancel();
        }

        public async Task<PlayableItem> RemoveFromQueue(int index)
        {
            try
            {
                if (index == Queue.Current)
                {
                    var it = Queue.RemoveFromQueue(index);
                    await Skip(0);
                    return it;
                }

                if (index >= Queue.Current) return Queue.RemoveFromQueue(index);
                {
                    var it = Queue.RemoveFromQueue(index);
                    Queue.Current -= 1;
                    return it;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Removing from queue failed: {e}", true, Debug.DebugColor.Error);
            }

            return null;
        }

        public async Task<PlayableItem> RemoveFromQueue(PlayableItem item)
        {
            var index = Queue.Items.IndexOf(item);
            try
            {
                if (index == Queue.Current)
                {
                    var it = Queue.RemoveFromQueue(item);
                    await Skip(0);
                    return it;
                }

                if (index >= Queue.Current) return Queue.RemoveFromQueue(item);
                {
                    var it = Queue.RemoveFromQueue(item);
                    Queue.Current -= 1;
                    return it;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Removing from queue failed: {e}", true, Debug.DebugColor.Error);
                return null;
            }
        }

        public async Task<PlayableItem> RemoveFromQueue(string name)
        {
            var item = Queue.GetWithString(name);
            var index = Queue.Items.IndexOf(item);
            try
            {
                if (index == Queue.Current)
                {
                    var it = Queue.RemoveFromQueue(item);
                    await Skip(0);
                    return it;
                }

                if (index >= Queue.Current) return Queue.RemoveFromQueue(item);
                {
                    var it = Queue.RemoveFromQueue(item);
                    Queue.Current -= 1;
                    return it;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Removing from queue failed: {e}", true, Debug.DebugColor.Error);
                return null;
            }
        }

        public void Disconnect(string message = "Bye! \\(◕ ◡ ◕\\)")
        {
            _timer.Stop();
            var task = new Task(async () =>
            {
                await WebSocketManager.SendDying();
                await Statusbar.UpdateMessageAndStop(message);
            });
            task.Start();
            Die = true;
            FfMpeg.KillSync();
            Connection.Disconnect();
            Sink.Dispose();
            lock (Manager.Main)
            {
                if (Manager.Main.Contains(this)) Manager.Main.Remove(this);
            }

            Debug.Write($"Disconnecting from channel: {VoiceChannel.Name} in guild: {CurrentGuild.Name}");
        }

        public async Task DisconnectAsync(string message = "Bye! \\(◕ ◡ ◕\\)")
        {
            try
            {
                _timer.Stop();
                await WebSocketManager.SendDying();
                await Statusbar.UpdateMessageAndStop(message);
                Die = true;
                FfMpeg.KillSync();
                CancelSource.Cancel();
                Sink.Dispose();
                Connection.Disconnect();
                lock (Manager.Main)
                {
                    if (Manager.Main.Contains(this)) Manager.Main.Remove(this);
                }

                await Debug.WriteAsync(
                    $"Disconnecting from channel: {VoiceChannel.Name} in guild: {CurrentGuild.Name}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Disconnect Async Failed: {e}", true, Debug.DebugColor.Urgent);
            }
        }

        public void Shuffle()
        {
            Queue.Shuffle();
        }

        public PlayableItem GoToIndex(int index)
        {
            Paused = false;
            if (index >= Queue.Count && index < -1) return null;
            Queue.Current = index - 1;
            FfMpeg.KillSync();
            return Queue.GetNext();
        }

        public void Pause()
        {
            Paused = !Paused;
            if (!Paused) return;
            Queue.Current -= 1;
            FfMpeg.KillSync();
            Stopwatch.Stop();
        }

        public Controllers.Bot.PlayerInfo ToPlayerInfo()
        {
            var stats = new Controllers.Bot.PlayerInfo();
            try
            {
                stats.Title = CurrentItem.GetTitle();
                stats.Author = CurrentItem.GetAuthor();
                stats.Current = Stopwatch.Elapsed.ToString("hh\\:mm\\:ss");
                stats.Total = TimeSpan.FromMilliseconds(CurrentItem.GetLength())
                    .ToString("hh\\:mm\\:ss");
                stats.TotalDuration = CurrentItem.GetLength();
                stats.CurrentDuration = (ulong) Stopwatch.ElapsedMilliseconds;
                stats.Loop = LoopStatus switch
                {
                    Loop.None => "None", Loop.One => "One", Loop.WholeQueue => "WholeQueue",
                    _ => "bad"
                };
                stats.ThumbnailUrl = CurrentItem.GetThumbnailUrl();
                stats.Paused = Paused;
                stats.Index = Queue.Items.ToList().IndexOf(CurrentItem);
            }
            catch
            {
                Debug.Write("Failed to serialize Player.cs stats.");
                return new Controllers.Bot.PlayerInfo();
            }

            return stats;
        }

        ~Player()
        {
            try
            {
                lock (Manager.Main)
                {
                    Disconnect();
                    if (Manager.Main.Contains(this)) Manager.Main.Remove(this);
                }
            }
            catch (Exception e)
            {
                Debug.Write($"Failed to disconnect from destructor in Player.cs: \"{e}\"", true,
                    Debug.DebugColor.Urgent);
            }
        }
    }
}