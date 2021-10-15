using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Audio.Platforms;
using Bat_Tosho.Enums;
using Bat_Tosho.Messages;
using Bat_Tosho.Methods;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace Bat_Tosho.Audio
{
    public static class Manager
    {
        public static readonly Dictionary<DiscordGuild, Instance> Instances = new();
        public static readonly Dictionary<string, DiscordUser> WebUserInterfaceUsers = new();

        public static Dictionary<DiscordUser, bool> AbuseList = new();
        public static bool ExecutedCommand { get; set; }

        public static async Task Play(CommandContext ctx, string path)
        {
            var statusbarChannel = ReturnStatusbarChannel(ctx);
            DiscordMessage message =
                await ctx.Client.SendMessageAsync(statusbarChannel, "```Hello! I need to do some things. Please don't queue up any songs until I am done.```");
            //Adding Guild to Dictionary
            if (!PrepareGuild(ctx))
            {
                await Respond.FormattedMessage(ctx,
                    "Error in preparation in joining. Failed to add guild to instances.");
                throw new InvalidProgramException();
            }

            var instance = Instances[ctx.Guild];
            //Checking Caller VoiceState
            var userVoiceStateChannel = ctx.Member.VoiceState?.Channel;
            if (userVoiceStateChannel == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a voice channel before using the play command.```");
                await message.DeleteAsync();
                throw new ArgumentNullException(nameof(userVoiceStateChannel));
            }

            var search = new Search(ctx);

            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            List<VideoInformation> results;
            instance.Player = new Player();
            switch (instance.Playing)
            {
                case true:
                    if (instance.CurrentVideoInfo().Paused)
                    {
                        instance.CurrentVideoInfo().Paused = false;
                        if (string.IsNullOrEmpty(path))
                            return;
                    }

                    instance.UpdatingLists = true;
                    results = await search.GetResults(path);
                    if (results == null)
                    {
                        await message.ModifyAsync(
                            "```Couldn't find this video. Please try with searching with a more accurate search term or the target video id.```");
                        return;
                    }

                    instance.VideoInfos.AddRange(results);
                    if (results.Count == 1)
                        await message.ModifyAsync($"```Added: {results[0].Name} - {results[0].Author}```");
                    else
                        await message.ModifyAsync($"```Added: {path}```");
                    instance.UpdatingLists = false;
                    if (instance.ActiveDownloadTasks > 0) break;
                    instance.ActiveDownloadTasks = 1;
                    await instance.Player.DownloadRemaining(instance);
                    instance.ActiveDownloadTasks = 0;
                    break;

                case false:
                    instance.Playing = true;
                    instance.Statusbar = new Statusbar();
                    instance.StatusbarChannel = statusbarChannel;
                    instance.StatusbarMessage = message;
                    instance.UpdatingLists = true;
                    instance.VoiceChannel = userVoiceStateChannel;
                    
                    results = await search.GetResults(path);
                    var statusbarTask = new Task(async () =>
                    {
                        await Instances[ctx.Guild].Statusbar.Update(ctx, Instances[ctx.Guild]);
                    });
                    statusbarTask.Start();
                    Instances[ctx.Guild].Statusbar.Status = StatusbarStatus.AddingVideos;
                    if (results == null)
                    {
                        Instances[ctx.Guild].Statusbar.Status = StatusbarStatus.Null;
                        await Instances[ctx.Guild].Statusbar.Stop();
                        await message.ModifyAsync(
                            "```Couldn't find this video. Please try with searching with a more accurate search term or the target video id.```");
                        Instances.Remove(ctx.Guild);
                        return;
                    }

                    if (results.Any(vi => vi.YoutubeIdOrPathToFile is "OjNpRbNdR7E" or "3IcMXj-x7Io" or "Wy9ErjEMYa8"))
                        await GloryToTheCCP(ctx);
                    instance.VideoInfos.AddRange(results);
                    if (vnc is not null) throw new SystemException();
                    var connection = await vnext.ConnectAsync(userVoiceStateChannel);
                    var transmit = connection.GetTransmitSink();
                    instance.TransmitSink = transmit;
                    instance.UpdatingLists = false;
                    Instances[ctx.Guild].Statusbar.Status = StatusbarStatus.Playing;
                    try
                    {
                        await instance.Player.PlayerInstance(ctx, transmit);
                    }
                    catch (Exception e)
                    {
                        await Debug.Write($"Player instance failed: {e}");
                        throw;
                    }

                    Instances[ctx.Guild].VideoInfos.Clear();
                    await Instances[ctx.Guild].Statusbar.Stop(Instances[ctx.Guild], @"Bye! \(◕ ◡ ◕\) ");
                    connection.Disconnect();
                    instance.Playing = false;
                    Instances.Remove(ctx.Guild);
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        private static async Task GloryToTheCCP(CommandContext ctx)
        {
            try
            {
                var channel = await ctx.Member.CreateDmChannelAsync();
                await ctx.Client.SendMessageAsync(channel, "```Good job citizen. Glory to the CCP.```");
            }
            catch (Exception e)
            {
                await Debug.Write($"Honoring the CPP failed: {e}");
            }
        }

        public static async Task Pause(CommandContext ctx)
        {
            if (!Instances.ContainsKey(ctx.Guild)) return;
            if (Instances[ctx.Guild].VideoInfos.Count == 0) return;
            if (Instances[ctx.Guild].CurrentVideoInfo().Paused)
            {
                Instances[ctx.Guild].CurrentVideoInfo().Paused = false;
                return;
            }

            Instances[ctx.Guild].CurrentVideoInfo().Paused = true;
            Instances[ctx.Guild].CurrentVideoInfo().Stopwatch.Stop();
            await Instances[ctx.Guild].Ffmpeg.Kill(true);
            Instances[ctx.Guild].Song -= 1;
        }

        public static async Task Leave(CommandContext ctx, bool check = true)
        {
            Instances[ctx.Guild].CurrentVideoInfo().Stopwatch.Reset();
            Instances[ctx.Guild].Playing = false;
            if (check)
            {
                var channel = ctx.Member.VoiceState?.Channel;
                if (channel == null)
                    throw new InvalidProgramException(
                        $"{ctx.User.Username} in {ctx.Guild.Name} is not in a voice channel.");
            }

            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            vnc.Disconnect();
            await Instances[ctx.Guild].Statusbar.Stop();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wave:"));
            Instances[ctx.Guild].VideoInfos.Clear(); // Just in case something breaks.
            await Instances[ctx.Guild].Statusbar.Stop(Instances[ctx.Guild], @"Bye! \(◕ ◡ ◕\) ");
            Instances.Remove(ctx.Guild);
        }

        public static async Task Skip(CommandContext ctx, int times = 1)
        {
            try
            {
                if (Instances[ctx.Guild].WaitingToLeave)
                    return;
                Instances[ctx.Guild].CurrentVideoInfo().Stopwatch.Reset();
                Instances[ctx.Guild].CurrentVideoInfo().Paused = false;
                Instances[ctx.Guild].Song += times - 1;
                
                if (Instances[ctx.Guild].Song < -1)
                    Instances[ctx.Guild].Song = -1;
                if (Instances[ctx.Guild].Song > Instances[ctx.Guild].VideoInfos.Count - 1)
                    Instances[ctx.Guild].Song = Instances[ctx.Guild].VideoInfos.Count - 1;
                await Instances[ctx.Guild].Ffmpeg.Kill();
            }
            catch (Exception e)
            {
                await Debug.Write($"Skip Exception {e}");
                throw;
            }
        }

        public static async Task Shuffle(CommandContext ctx)
        {
            while (Instances[ctx.Guild].UpdatingLists) await Task.Delay(333);
            var tempLVi = Instances[ctx.Guild].VideoInfos;
            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            switch (vnc)
            {
                case not null:
                    Program.Rng = new Random();
                    tempLVi = Instances[ctx.Guild].VideoInfos.OrderBy
                        (_ => Program.Rng.Next()).ToList();
                    var tempVi = Instances[ctx.Guild].CurrentVideoInfo();
                    tempLVi.Remove(tempVi);
                    tempLVi.Insert(0, tempVi);
                    break;
            }

            Instances[ctx.Guild].VideoInfos = tempLVi;
            Instances[ctx.Guild].Song = 0;
        }

        public static async Task Loop(CommandContext ctx)
        {
            var instance = Instances[ctx.Guild];

            instance.LoopStatus = instance.LoopStatus switch {LoopStatus.None => LoopStatus.LoopPlaylist, LoopStatus.LoopPlaylist => LoopStatus.LoopOne,
                LoopStatus.LoopOne => LoopStatus.None, _ => LoopStatus.None
            };
            await Respond.FormattedMessage(ctx, "Loop status is now: " +
            $"{instance.LoopStatus switch {LoopStatus.None => "None", LoopStatus.LoopPlaylist => "Looping whole playlist", LoopStatus.LoopOne => "Looping one song.", _ => throw new NotSupportedException()}}");
        }
        
        private static bool PrepareGuild(CommandContext ctx)
        {
            try
            {
                if (Instances.ContainsKey(ctx.Guild)) return true;
                Instances.Add(ctx.Guild, new Instance());
                return true;
            }
            catch (Exception e)
            {
                Debug.Write($"Preparing Guild Failed. {e}").RunSynchronously();
                return false;
            }
        }

        public static async Task Remove(CommandContext ctx, int index)
        {
            try
            {
                if (index >= 1 && index < Instances[ctx.Guild].VideoInfos.Count)
                {
                    await Respond.FormattedMessage(ctx,
                        $"Removing song: ({index+1}) {Instances[ctx.Guild].VideoInfos[index].Name} - {Instances[ctx.Guild].VideoInfos[index].Author}.");
                    Instances[ctx.Guild].VideoInfos.RemoveAt(index);
                }
            }
            catch (Exception e)
            {
                await Debug.Write($"Remove Exception {e}");
                throw;
            }
        }

        public static async Task Move(CommandContext ctx, int oldIndex, int newIndex)
        {
            await Respond.FormattedMessage(ctx,
                $"Moving: ({oldIndex}) {Instances[ctx.Guild].VideoInfos[oldIndex - 1].Name} - {Instances[ctx.Guild].VideoInfos[oldIndex - 1].Author} to {newIndex}.");
            var videoInfo = Instances[ctx.Guild].VideoInfos[oldIndex - 1];
            Instances[ctx.Guild].VideoInfos.Remove(videoInfo);
            Instances[ctx.Guild].VideoInfos.Insert(newIndex - 1, videoInfo);
        }
        
        public static void Clear(DiscordGuild guild)
        {
            var ins = Instances[guild];
            var currentVi = Instances[guild].CurrentVideoInfo();
            ins.VideoInfos.Clear();
            ins.VideoInfos.Add(currentVi);
            ins.Song = 0;
        }
        public static async Task Clear(CommandContext ctx)
        {
            var guild = ctx.Guild;
            var ins = Instances[guild];
            var currentVi = Instances[guild].CurrentVideoInfo();
            ins.VideoInfos.Clear();
            ins.VideoInfos.Add(currentVi);
            ins.Song = 0;
            await Respond.FormattedMessage(ctx, "Sucessfully cleared the playlist.");
        }
        
        public static async Task SetVolume(CommandContext ctx, double volume)
        {
            var voiceState = ctx.Member.VoiceState?.Channel;
            if (voiceState == null) await Respond.FormattedMessage(ctx, "Join a channel to use the volume command.");
            switch (volume)
            {
                case < 0:
                    await Respond.FormattedMessage(ctx, "Volume cannot be lower than 0%.");
                    return;
                case > 250:
                    await Respond.FormattedMessage(ctx, "Volume cannot be higher than 250%.");
                    return;
            }

            if (!Instances.ContainsKey(ctx.Guild)) await Respond.FormattedMessage(ctx, "I am not in the channel.");

            var instance = Instances[ctx.Guild];
            instance.TransmitSink.VolumeModifier = volume / 100;
        }

        public static async Task Lyrics(CommandContext ctx, string text)
        {
            var webClient = new WebClient();
            string q;
            switch (string.IsNullOrEmpty(text))
            {
                case true:
                    var videoInfo = Instances[ctx.Guild].VideoInfos[Instances[ctx.Guild].Song];
                    q = $"{Regex.Replace(Regex.Replace(videoInfo.Name, @"\([^()]*\)", ""), @"\[[^]]*\]", "")}" +
                        $"{videoInfo.Name.Contains("-") switch {true => "", false => $" - {Regex.Replace(Regex.Replace(videoInfo.Author, "- Topic", ""), @"\([^()]*\)", "")}"}}";
                    break;
                case false:
                    q = text;
                    break;
            }

            await Debug.Write($"Lyrics Search Term is: {q}");
            const string apiKey = "ce7175JINJTgC94aJFgeiwa7Bh99EaoqZFhTeFV9ejmpO2qjEXOpi1eR";
            var response = await webClient.DownloadStringTaskAsync(
                $"https://api.happi.dev/v1/music?q={q.Replace(" ", "%20")}&limit=5&apikey={apiKey}&type=track&lyrics=1");
            var apiMusicResponse = JsonSerializer.Deserialize<HappiApiMusicResponse>(response);
            if (apiMusicResponse is null) throw new InvalidOperationException();
            response = await webClient.DownloadStringTaskAsync(
                $"{apiMusicResponse.result.First().api_lyrics}?apikey={apiKey}");
            var lyricsResponse = JsonSerializer.Deserialize<HappiApiLyricsResponse>(response);
            if (lyricsResponse is null) throw new InvalidOperationException();
            var message =
                $"{lyricsResponse.result.track} - {lyricsResponse.result.artist} Lyrics: \n{lyricsResponse.result.lyrics}";
            if (message.Length > 2000) throw new InvalidDataException();
            await Respond.FormattedMessage(ctx, message);
            Instances[ctx.Guild].Statusbar.UpdatePlacement = true;
        }

        public static async Task PlayNext(CommandContext ctx, string path)
        {
            var instance = Instances[ctx.Guild];
            var search = new Search(ctx);
            var results = await search.GetResults(path);
            instance.VideoInfos.InsertRange(instance.Song + 1, results);
            
            await Respond.FormattedMessage(ctx, $"Added: {results.Count switch {1 => $"{results[0].Name} - {results[0].Author}", >2 => path, _ => $"{path}, but there may be errors idk."}}");
        }
        public static async Task Ungag(CommandContext ctx, DiscordUser user)
        {
            await Debug.Write("Ungag command started.");
            await Debug.Write($"User: {user}.");
            if (AbuseList.ContainsKey(user))
            {
                AbuseList[user] = false;
                return;
            }
            AbuseList.Add(user, true);

            bool running = true;
            while (running)
            {
                await Debug.Write($"Abuse List Count: {AbuseList.Count}");
                if (AbuseList.Count == 0)
                    running = false;
                foreach (var abusedChild in AbuseList.Where(bo => bo.Value))
                {
                    var member = ctx.Guild.Members[abusedChild.Key.Id];
                    await member.SetMuteAsync(false);
                    await Task.Delay(1200);
                }
            }
        }

        public static async Task GetWebUi(CommandContext ctx)
        {
            var dm = await ctx.Member.CreateDmChannelAsync();
            if (WebUserInterfaceUsers.ContainsValue(ctx.User))
            {
                string code = WebUserInterfaceUsers.First(we => we.Value == ctx.User).Key;
                await ctx.Client.SendMessageAsync(dm, $"```You have already generated a Web Ui Code. Your Web UI Code is: {code}\n```https://dank.gq/BatTosho?guildId={ctx.Guild.Id}&clientSecret={code}");
                return;
            }
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-.";
            var random = new Random(Program.Rng.Next(int.MaxValue));
            string passwordString = new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            await ctx.Client.SendMessageAsync(dm, $"```Your Web UI Code is: {passwordString}\n```https://dank.gq/BatTosho?guildId={ctx.Guild.Id}&clientSecret={passwordString}");
            WebUserInterfaceUsers.Add(passwordString, ctx.User);
        }

        private static DiscordChannel ReturnStatusbarChannel(CommandContext ctx) =>
            ctx.Guild.Channels.FirstOrDefault(ch => ch.Value.Name is "diskoteka" or "discoteka" or "music").Value ?? ctx.Channel;
        public static async Task Download(CommandContext ctx, string path)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());
            StreamManifest streamManifest;
            string id;
            switch (path.Contains("http://") || path.Contains("https://"))
            {
                case true:
                    id = path.Split("shorts/").Last()
                        .Split("watch?v=").Last().Split(".be/").Last().Split("&").First().Split("?").Last();
                    streamManifest = await client.Videos.Streams.GetManifestAsync(id);
                    break;
                case false:
                    var results = await client.Search.GetVideosAsync(path).CollectAsync(10);
                    var range = results.Count switch {<10 => results.Count, _ => 10};
                    var msg = $"Results for {path}: \n";
                    for (var index = 0; index < range; index++)
                    {
                        var res = results[index];
                        msg += $"({index}) {res.Title} - {res.Author}\n";
                    }

                    msg += "\n Choose a result.";
                    await Debug.Write(msg);
                    await ctx.RespondAsync(msg);
                    var response = await ctx.Message.Channel.GetNextMessageAsync();
                    var choice = Convert.ToInt32(response.Result.Content);
                    streamManifest =
                        await client.Videos.Streams.GetManifestAsync(
                            Debug.WriteAndReturnString(results[choice].Id, "Stream Manifest Id is"));
                    id = results[Convert.ToInt32(response.Result.Content)].Id;
                    break;
            }

            var message = "Choose a stream: \n";
            var videoInfos = streamManifest.GetVideoOnlyStreams().ToArray();
            var audioInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            for (var index = 0; index < videoInfos.Length; index++)
            {
                var voInfo = videoInfos[index];
                message += $"({index}) Resolution: {voInfo.VideoQuality.Label} - Codec: {voInfo.VideoCodec}\n";
            }

            await Debug.Write(message);
            await ctx.RespondAsync(message);
            var result = await ctx.Message.Channel.GetNextMessageAsync();
            var videoInfo = videoInfos[Convert.ToInt32(result.Result.Content)];

            var videoPath = $"{Program.MainDirectory}dll/VideoDownloader/{id}_video.{videoInfo.Container}";
            var audioPath = $"{Program.MainDirectory}dll/VideoDownloader/{id}_audio.{audioInfo.Container}";
            var destination = $"/srv/http/Bat_Tosho_Content/Youtube/{id}.{videoInfo.Container}";
            await client.Videos.Streams.DownloadAsync(videoInfo, videoPath);
            await client.Videos.Streams.DownloadAsync(audioInfo, audioPath);
            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments =
                        $"-v quiet -nostats -i {videoPath} -i {audioPath} -c copy -map 0:v:0 -map 1:a:0 {destination}",
                    UseShellExecute = false
                }
            };
            ffmpegProcess.Start();
            await ffmpegProcess.WaitForExitAsync();
            await ctx.RespondAsync(
                $"```Downloaded video. Link: ```https://dank.gq/Bat_Tosho_Content/Youtube/{id}.{videoInfo.Container}");
            File.Delete(videoPath);
            File.Delete(audioPath);
        }

        public static async Task List(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            switch (vnc)
            {
                case not null: break;
                case null: return;
            }

            var list = Instances[ctx.Guild].VideoInfos;
            var strBuilder = new StringBuilder();
            for (var i = 0; i < list.Count; i++) strBuilder.AppendLine($"({i + 1}): {list[i].Name} - {list[i].Author}");

            var pages = ctx.Client.GetInteractivity().GeneratePagesInEmbed(strBuilder.ToString());

            await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages, null,
                PaginationBehaviour.WrapAround, PaginationDeletion.DeleteEmojis);
        } // ReSharper disable InconsistentNaming
        private record HappiApiResponseMusic (string track, int id_track, string artist, bool hasLyrics, int id_artist,
            string album, int bpm, int id_album, string cover, string lang, string api_artist, string api_albumsm,
            string api_album, string api_tracks, string api_track, string api_lyrics);

        private record HappiApiResponseLyrics (string artist, int id_artist, string track, int id_track, int id_album,
            string album, string lyrics, string api_artist, string api_albums,
            string api_album, string api_tracks, string api_track, string api_lyrics, string lang,
            string copyright_label, string copyright_notice, string copyright_text);

        private record HappiApiMusicResponse(bool success, int length, HappiApiResponseMusic[] result);

        private record HappiApiLyricsResponse(bool success, int length, HappiApiResponseLyrics result);
        
    }
}