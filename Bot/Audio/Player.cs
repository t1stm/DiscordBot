using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Enums;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio
{
    public class Player
    {
        private readonly Timer _timer = new()
        {
            Interval = 1000
        };

        public FfMpeg FfMpeg = new();
        private ElapsedEventHandler Handler;
        public bool WaitingToLeave { get; set; }
        public bool Started { get; set; } = false;
        public DiscordChannel VoiceChannel { get; set; }
        public bool Paused { get; set; }
        public bool Normalize { get; set; } = true;
        public Queue Queue { get; } = new();
        public Statusbar Statusbar { get; set; } = new();
        public DiscordChannel Channel { get; set; }
        public DiscordClient CurrentClient { get; set; }
        public DiscordGuild CurrentGuild { get; set; }
        public VoiceTransmitSink Sink { get; set; }
        public VoiceNextConnection Connection { get; set; }
        public Loop LoopStatus { get; set; } = Loop.None;
        private bool BreakNow { get; set; }
        public IPlayableItem CurrentItem { get; set; }
        public Stopwatch Stopwatch { get; } = new();
        public Stopwatch WaitingStopwatch { get; } = new();

        public async Task Play(int current = 0)
        {
            Statusbar.Client = CurrentClient;
            Statusbar.Guild = CurrentGuild;
            Statusbar.Player = this;
            Statusbar.Channel = Channel;
            var statusbar = new Task(async () =>
            {
                await Statusbar.Start();
            });
            statusbar.Start();
            Handler = async (_, _) => { await Queue.DownloadAll(); };
            _timer.Elapsed += Handler;
            _timer.Start();
            for (Queue.Current = current; Queue.Current < Queue.Count; Queue.Current++)
            {
                CurrentItem = Queue.GetCurrent();
                switch (CurrentItem)
                {
                    case SystemFile fi:
                        await PlayTrack(fi.Location, Stopwatch.Elapsed.ToString(@"c"));
                        break;
                    case SpotifyTrack tr:
                        var list = await new Search().Get(tr);
                        var track = list.First();
                        await track.Download();
                        await PlayTrack(track.Location, Stopwatch.Elapsed.ToString(@"c"));
                        break;
                    case YoutubeVideoInformation vi:
                        await vi.Download();
                        await PlayTrack(vi.Location, Stopwatch.Elapsed.ToString(@"c"));
                        break;
                }

                if (BreakNow)
                {
                    BreakNow = false;
                    Stopwatch.Stop();
                    break;
                }

                Stopwatch.Reset();
                if (LoopStatus == Loop.One) Queue.Current--;
                if (Queue.Current + 1 == Queue.Count && LoopStatus == Loop.WholeQueue) Queue.Current = -1;
                if (Queue.Current + 1 != Queue.Count) continue;
                WaitingToLeave = true;
                WaitingStopwatch.Start();
                while (WaitingToLeave)
                {
                    await Task.Delay(166);
                    if (Queue.Current + 1 >= Queue.Count && !(WaitingStopwatch.Elapsed.TotalMinutes >= 15)) continue;
                    WaitingToLeave = false;
                    WaitingStopwatch.Reset();
                }
            }

            _timer.Stop();
            _timer.Elapsed -= Handler;
        }

        private async Task PlayTrack(string location, string startingTime)
        {
            await Debug.WriteAsync($"Location is: {location}");
            Stopwatch.Start();
            await FfMpeg.ConvertAudioToPcm(location, startingTime, Normalize).CopyToAsync(Sink);
            await Sink.FlushAsync();
            await FfMpeg.Kill(false, false);
        }

        public async Task UpdateChannel(DiscordChannel channel)
        {
            CurrentClient.GetVoiceNext().GetConnection(CurrentGuild).Disconnect();
            Connection = await CurrentClient.GetVoiceNext().ConnectAsync(channel);
            Sink = Connection.GetTransmitSink();
            await Skip(0);
        }

        public Loop ToggleLoop() => LoopStatus = LoopStatus switch
        {
            Loop.None => Loop.WholeQueue, Loop.WholeQueue => Loop.One, Loop.One => Loop.None,
            _ => Loop.None
        };

        public async Task Skip(int times = 1)
        {
            Queue.Current += times - 1;
            await FfMpeg.Kill();
        }

        public void Disconnect()
        {
            Connection.Disconnect();
            _timer.Stop();
            var task = new Task(async () =>
            {
                await Statusbar.UpdateMessageAndStop("Bye!");
            });
            task.Start();
        }

        public void Shuffle()
        {
            Queue.Shuffle();
        }
    }
}