using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Controllers.Objects;
using BatToshoRESTApp.Enums;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Debug = BatToshoRESTApp.Methods.Debug;
using Timer = System.Timers.Timer;

namespace BatToshoRESTApp.Audio
{
    public class Player
    {
        private readonly Timer _timer = new()
        {
            Interval = 1000
        };

        private FfMpeg FfMpeg { get; set; }= new();
        private ElapsedEventHandler Handler { get; set; }
        public bool WaitingToLeave { get; set; }
        public bool Started { get; set; }
        public DiscordChannel VoiceChannel { get; set; }
        public bool Paused { get; set; }
        public bool Normalize { get; set; } = true;
        private CancellationTokenSource CancelSource { get; set; } = new();
        public Queue Queue { get; } = new();
        public Statusbar Statusbar { get; set; } = new();
        public DiscordChannel Channel { get; set; }
        public DiscordClient CurrentClient { get; init; }
        public DiscordGuild CurrentGuild { get; set; }
        public VoiceTransmitSink Sink { get; set; }
        public VoiceNextConnection Connection { get; set; }
        public bool UpdatedChannel { get; set; } = false;
        public Loop LoopStatus { get; private set; } = Loop.None;
        private bool BreakNow { get; set; }
        public IPlayableItem CurrentItem { get; private set; }
        public Stopwatch Stopwatch { get; } = new();
        public Stopwatch WaitingStopwatch { get; } = new();
        public int VoiceUsers { get; set; }
        public bool Die { get; set; }

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
                    await Debug.WriteAsync($"VoiceSocket Errored in Guild: \"{CurrentGuild.Name}\" with arguments \"{args.Exception}\"");
                };
                var statusbar = new Task(async () => { await Statusbar.Start(); });
                statusbar.Start();
                Handler = async (_, _) => { await Queue.DownloadAll(); };
                _timer.Elapsed += Handler;
                _timer.Start();
                #pragma warning disable 618
                Bot.WebSocketServer.AddWebSocketService($"/{VoiceChannel.Id}", () => new WebSock(this));
                #pragma warning restore 618
                for (Queue.Current = current; Queue.Current < Queue.Count; Queue.Current++)
                {
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
                            var list = await new Search().Get(tr);
                            var track = list.First();
                            await track.Download();
                            Queue.Current -= 1;
                            continue;
                        
                        case YoutubeVideoInformation vi:
                            while (Stopwatch.ElapsedMilliseconds < 1000)
                            {
                                await vi.Download();
                                await PlayTrack(CurrentItem.GetLocation(), Stopwatch.Elapsed.ToString(@"c"), true);
                            }
                            break;
                        
                        default:
                            await PlayTrack(CurrentItem.GetLocation(), Stopwatch.Elapsed.ToString(@"c"));
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

                _timer.Stop();
                _timer.Elapsed -= Handler;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Player Error: {e}");
                throw;
            }
        }

        private async Task PlayTrack(string location, string startingTime, bool isYoutube = false)
        {
            try
            {
                CancelSource = new CancellationTokenSource();
                UpdatedChannel = false;
                FfMpeg = new FfMpeg();
                try
                {
                    await Connection.SendSpeakingAsync();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Failed to set speaking to true: {e}");
                }
                await Debug.WriteAsync($"Location is: {location}");
                if (!Stopwatch.IsRunning) Stopwatch.Start();
                if (location is {Length: > 4} && location[..4] == "http" && isYoutube) await FfMpeg.UrlToPcm(location, startingTime, Normalize).CopyToAsync(Sink);
                else 
                    await FfMpeg.PathToPcm(location, startingTime, Normalize).CopyToAsync(Sink, null, CancelSource.Token);
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
                await Debug.WriteAsync($"PlayTrack failed: {e}");
            }
        }

        public bool UpdateVolume(float percent)
        {
            if (percent is > 200 or < 1) return false;
            Sink.VolumeModifier = percent / 100;
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
                        await Debug.WriteAsync("No Guild");
                        return;
                    }

                    await Debug.WriteAsync($"Current Voice Channel: {VoiceChannel.Id} - New: {channel.Id}");
                    VoiceChannel = channel;
                    var conn = CurrentClient.GetVoiceNext().GetConnection(CurrentGuild);
                    UpdatedChannel = true;
                    conn?.Disconnect();
                    var chan = CurrentGuild.Channels[channel.Id];
                    await Task.Delay(300);
                    Connection = await CurrentClient.GetVoiceNext().ConnectAsync(chan);
                    Sink = Connection.GetTransmitSink();
                    await Skip(0);
                    CancelSource.Cancel();
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Updating Channel Failed: {e}");
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
            Queue.Current += times - 1;
            await FfMpeg.Kill();
            await Sink.FlushAsync();
        }

        public void Disconnect(string message = "Bye! \\(◕ ◡ ◕\\)")
        {
            Bot.WebSocketServer.RemoveWebSocketService($"/{VoiceChannel.Id}");
            _timer.Stop();
            var task = new Task(async () => { await Statusbar.UpdateMessageAndStop(message); });
            task.Start();
            Die = true;
            FfMpeg.KillSync();
            Connection.Disconnect();
            Sink.Dispose();
        }

        public async Task DisconnectAsync(string message = "Bye! \\(◕ ◡ ◕\\)", bool isEvent = false)
        {
            try
            {
                Bot.WebSocketServer.RemoveWebSocketService($"/{VoiceChannel.Id}");
                _timer.Stop();
                await Statusbar.UpdateMessageAndStop(message);
                Die = true;
                FfMpeg.KillSync();
                Sink.Dispose();
                if (isEvent) return;
                Connection.Disconnect();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Disconnect Async Failed: {e}");
            }
        }

        public void Shuffle()
        {
            Queue.Shuffle();
        }

        public void GoToIndex(int index)
        {
            if (index >= Queue.Count && index < -1) return;
            Queue.Current = index - 1;
            FfMpeg.KillSync();
        }

        public void Pause()
        {
            Paused = !Paused;
            if (!Paused) return;
            Queue.Current -= 1;
            FfMpeg.KillSync();
            Stopwatch.Stop();
        }

        ~Player()
        {
            try
            {
                Disconnect();
            }
            catch (Exception e)
            {
                Debug.Write($"Failed to disconnect: {e}");
            }
        }
    }
}