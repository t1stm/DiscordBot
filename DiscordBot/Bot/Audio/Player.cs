#nullable enable
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
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Data.Models;
using DiscordBot.Enums;
using DiscordBot.Messages;
using DiscordBot.Objects;
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
        private ElapsedEventHandler? Handler { get; set; }
        private bool WaitingToLeave { get; set; }
        public bool Started { get; set; }
        public WebSocketManager WebSocketManager { get; }
        public DiscordChannel? VoiceChannel { get; set; }
        public bool Paused { get; set; }
        public GuildsModel Settings { get; set; } = new();

        public ILanguage Language
        {
            get => Parser.FromNumber(Settings.Language);
            set
            {
                Settings.Language = Parser.GetIndex(value);
                Settings.SetModified?.Invoke();
            }
        }

        public bool Normalize => Settings.Normalize;
        private CancellationTokenSource CancelSource { get; set; } = new();
        public Queue Queue { get; }
        public Statusbar Statusbar { get; } = new();
        public DiscordChannel? Channel { get; set; }
        public DiscordClient? CurrentClient { get; init; }
        public DiscordGuild? CurrentGuild { get; set; }
        public VoiceTransmitSink? Sink { get; set; }
        public VoiceNextConnection? Connection { get; set; }
        public bool UpdatedChannel { get; private set; }
        public Loop LoopStatus { get; set; } = Loop.None;
        private bool BreakNow { get; set; }
        public PlayableItem? CurrentItem { get; private set; }
        public Stopwatch Stopwatch { get; } = new();
        public Stopwatch WaitingStopwatch { get; } = new();
        public List<DiscordMember> VoiceUsers { get; set; } = new();
        private bool Die { get; set; }
        public static string StatusbarMessage { get; } = "";
        private double Volume { get; set; } = 100;
        public string? QueueToken { get; set; }
        public bool SavedQueue { get; set; }
        private object LockObject { get; } = new();

        public async Task Play(int current = 0)
        {
            try
            {
                QueueToken ??= Manager.GetFreePlaylistToken(CurrentGuild?.Id, VoiceChannel?.Id);
                if (Die) return;
                Statusbar.Client = CurrentClient;
                Statusbar.Guild = CurrentGuild;
                Statusbar.Player = this;
                Statusbar.Channel = Channel;
                if (Connection != null)
                    Connection.VoiceSocketErrored += async (_, args) =>
                    {
                        await Debug.WriteAsync(
                            $"VoiceSocket Errored in Guild: \"{CurrentGuild?.Name}\" with arguments \"{args.Exception}\"\n\nAttempting to reconnect.",
                            true, Debug.DebugColor.Urgent);
                        UpdateChannel(VoiceChannel);
                        await (CurrentClient?.SendMessageAsync(Channel,
                            Language.DiscordDidTheFunny().CodeBlocked()) ?? Task.CompletedTask);
                    };

                var statusbar = new Task(async () => { await Statusbar.Start(); });
                statusbar.Start();
                Handler = TimerEvent;
                _timer.Elapsed += Handler;
                _timer.Start();
                Queue.Current = current;

                do
                {
                    if (Queue.Current < 0)
                    {
                        Queue.Current++;
                        continue;
                    }

                    if (Die) break;
                    CurrentItem = Queue.GetCurrent();

                    if (CurrentItem != null)
                    {
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
                                var track = await Video.Search(tr);
                                lock (Queue.Items)
                                {
                                    Queue.Items[Queue.Current] = track;
                                }

                                CurrentItem = track;
                                await PlayTrack(track, Stopwatch.Elapsed.ToString(@"c"));
                                break;

                            default:
                                //await CurrentItem.Download();
                                await PlayTrack(CurrentItem, Stopwatch.Elapsed.ToString(@"c"));
                                break;
                        }
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
                    if (Queue.EndOfQueue)
                    {
                        await Task.Delay(166);
                        Statusbar.ChangeMode(StatusbarMode.Waiting);
                        if (!WaitingToLeave)
                        {
                            WaitingToLeave = true;
                            WaitingStopwatch.Start();
                        }

                        if (WaitingStopwatch.Elapsed.TotalMinutes > 15) Die = true;
                        continue;
                    }

                    Queue.Current++;
                    WaitingStopwatch.Reset();
                    WaitingToLeave = false;
                } while (!Die);

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

        private async void TimerEvent(object? o, ElapsedEventArgs elapsedEventArgs)
        {
            await Queue.ProcessAll();
            await WebSocketManager.BroadcastCurrentTime();
        }

        public void SaveCurrentQueue()
        {
            try
            {
                QueueToken ??= Manager.GetFreePlaylistToken(CurrentGuild?.Id, VoiceChannel?.Id);
                lock (Queue.Items)
                {
                    SharePlaylist.Write(QueueToken, Queue.Items);
                }

                SavedQueue = true;
            }
            catch (Exception e)
            {
                Debug.Write($"Failed to save queue: \"{e}");
            }
        }

        private async Task PlayTrack(PlayableItem item, string startingTime)
        {
            try
            {
                CancelSource = new CancellationTokenSource();
                UpdatedChannel = false;
                lock (LockObject)
                {
                    CurrentItem = item;
                }

                FfMpeg = new FfMpeg();
                try
                {
                    await WebSocketManager.BroadcastCurrentItem();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Failed to broadcast item: \"{e}\"");
                }

                if (!Stopwatch.IsRunning) Stopwatch.Start();
                UpdateVolume();
                switch (item)
                {
                    // Temporary fix to be able to listen to ABBA and Bon Jovi... I know...
                    case MusicObject obj when obj.GetLocation().ToLower().EndsWith(".wv"):
                    case YoutubeVideoInformation vi when vi.GetIfLiveStream():
                    case TwitchLiveStream:
                        await FfMpeg.PathToPcm(item.GetLocation(), startingTime, Normalize)
                            .CopyToAsync(Sink, null, CancelSource.Token);
                        break;
                    case SpotifyTrack track:
                        var playable = await Search.GetSingle(track);
                        await PlayTrack(playable, startingTime);
                        return;
                    default:
                        await FfMpeg.ItemToPcm(item, Sink, startingTime, Normalize);
                        break;
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
            if (Sink == null) return true;
            Sink.VolumeModifier = (Volume = percent.Value) / 100;
            return true;
        }

        public void UpdateChannel(DiscordChannel? channel)
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
                        await (CurrentClient?.SendMessageAsync(Channel,
                            Language.DiscordDidTheFunny().CodeBlocked()) ?? Task.CompletedTask);
                    };
                    Sink = Connection.GetTransmitSink();
                    await Skip(0);
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
            await (Sink?.FlushAsync() ?? Task.CompletedTask);
            CancelSource.Cancel();
        }

        public void PlsFix() // Funny Name
        {
            Queue.Current -= 1;
            FfMpeg.KillSync();
            CancelSource.Cancel();
        }

        public async Task<PlayableItem?> RemoveFromQueue(int index)
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

        public async Task<PlayableItem?> RemoveFromQueue(PlayableItem item)
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

        public async Task<PlayableItem?> RemoveFromQueue(string name)
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
            if (Settings.SaveQueueOnLeave) SaveCurrentQueue();
            _timer.Stop();
            var task = new Task(async () =>
            {
                await WebSocketManager.SendDying();
                await Statusbar.UpdateMessageAndStop(message + (Settings.SaveQueueOnLeave
                    ? $"\n\n{Language.SavedQueueAfterLeavingMessage($"-p pl:{QueueToken}")}"
                    : ""));
            });
            task.Start();
            Die = true;
            FfMpeg.KillSync();
            Connection?.Disconnect();
            Sink?.Dispose();
            lock (Manager.Main)
            {
                if (Manager.Main.Contains(this)) Manager.Main.Remove(this);
            }

            Debug.Write($"Disconnecting from channel: {VoiceChannel?.Name} in guild: {CurrentGuild?.Name}");
        }

        public async Task DisconnectAsync(string message = "Bye! \\(◕ ◡ ◕\\)")
        {
            try
            {
                if (Settings.SaveQueueOnLeave) SaveCurrentQueue();
                _timer.Stop();
                await WebSocketManager.SendDying();
                await Statusbar.UpdateMessageAndStop(message);
                Die = true;
                FfMpeg.KillSync();
                CancelSource.Cancel();
                Sink?.Dispose();
                Connection?.Disconnect();
                lock (Manager.Main)
                {
                    if (Manager.Main.Contains(this)) Manager.Main.Remove(this);
                }

                await Debug.WriteAsync(
                    $"Disconnecting from channel: {VoiceChannel?.Name} in guild: {CurrentGuild?.Name}");
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

        public PlayableItem? GoToIndex(int index)
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

        public PlayerInfo? ToPlayerInfo()
        {
            var stats = new PlayerInfo();
            try
            {
                if (CurrentItem == null) return null;
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
                return new PlayerInfo();
            }

            return stats;
        }

        ~Player()
        {
            try
            {
                lock (Manager.Main)
                {
                    Debug.Write("Destructor was called in Player.cs");
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