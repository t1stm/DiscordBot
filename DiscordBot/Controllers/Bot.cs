#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms.Spotify;
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Data;
using DiscordBot.Enums;
using DiscordBot.Methods;
using DiscordBot.Objects;
using DSharpPlus;
using Microsoft.AspNetCore.Mvc;
using YoutubePlaylist = DiscordBot.Audio.Platforms.Youtube.Playlist;


namespace DiscordBot.Controllers
{
    public class Bot : Controller
    {
        public static List<User> WebUiUsers { get; set; } = new();

        public static async Task LoadUsers(bool display = false)
        {
            try
            {
                var users = Databases.UserDatabase.ReadCopy();
                lock (WebUiUsers)
                {
                    WebUiUsers = users.Select(r => new User(r)).ToList();
                }

                if (display) PrintUsers();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Failed to load WebUI users: \"{e}\"");
            }
        }

        public static void PrintUsers()
        {
            foreach (var user in WebUiUsers) Debug.Write($"Id: {user.Id}, Secret: {user.Token}");
        }

        public static async Task AddUser(ulong userId, string clientSecret)
        {
            var user = await Objects.User.FromId(userId);
            user.Token = clientSecret;
            await LoadUsers();
        }

        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                var res = await Audio.Platforms.Search.Get(searchTerm, returnAllResults: true);
                var items = res?.Select(r => r.ToSearchResult()).ToList();
                return Json(items);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"API: Search Failed: \"{e}\"");
                return Ok("503");
            }
        }

        public IActionResult GetAvailableGuilds(string clientSecret)
        {
            if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
            var search = WebUiUsers.Get(clientSecret);
            if (search == null) return Ok("403");

            try
            {
                var guilds = DiscordBot.Bot.Clients[0].Guilds.Values
                    .Where(gi => gi.Members.ContainsKey(search.Id));
                var items = guilds.Select(g => new GuildItem {Id = g.Id + "", Name = g.Name, IconUrl = g.IconUrl})
                    .ToList();
                return Json(items);
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public async Task<IActionResult> GetChannelsInGuild(ulong id, string clientSecret)
        {
            if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
            var search = WebUiUsers.Get(clientSecret);
            if (search == null) return Ok("403");

            try
            {
                var guild = DiscordBot.Bot.Clients[0].Guilds.First(g => g.Value.Id == id).Value;
                var channels = await guild.GetChannelsAsync();
                var items = channels.Where(ch => ch.Type == ChannelType.Voice)
                    .Where(ch => Manager.Main.Any(pl => pl.VoiceChannel?.Id == ch.Id))
                    .Select(g => new GuildItem {Id = g.Id + "", Name = g.Name}).ToList();
                return Json(items);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in GetChannelsInGuild: {e}");
                return Ok("503");
            }
        }

        public async Task<IActionResult> GetUserStats(string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                var search = WebUiUsers.Get(clientSecret);
                if (search == null) return Ok("403");

                var user = await DiscordBot.Bot.Clients[0].GetUserAsync(search.Id, true);
                var info = new UserInfo
                {
                    Username = user.Username,
                    Discriminator = user.Discriminator,
                    ImageUrl = user.GetAvatarUrl(ImageFormat.Auto)
                };
                return Json(info);
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public IActionResult GetPlayerStats(ulong channelId)
        {
            try
            {
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                var stats = new PlayerInfo();
                try
                {
                    stats.Title = player.CurrentItem?.GetTitle();
                    stats.Author = player.CurrentItem?.GetAuthor();
                    stats.Current = player.Stopwatch.Elapsed.ToString("hh\\:mm\\:ss");
                    stats.Total = TimeSpan.FromMilliseconds(player.CurrentItem?.GetLength() ?? 0)
                        .ToString("hh\\:mm\\:ss");
                    stats.TotalDuration = player.CurrentItem?.GetLength() ?? 0;
                    stats.CurrentDuration = (ulong) player.Stopwatch.ElapsedMilliseconds;
                    stats.Loop = player.LoopStatus switch
                    {
                        Loop.None => "None", Loop.One => "One", Loop.WholeQueue => "WholeQueue",
                        _ => "bad"
                    };
                    stats.ThumbnailUrl = player.CurrentItem?.GetThumbnailUrl();
                    stats.Paused = player.Paused;
                    stats.Index = player.CurrentItem == null ? 0 : player.Queue.Items.ToList().IndexOf(player.CurrentItem);
                }
                catch (Exception e)
                {
                    Debug.Write($"Error in generating current song information for web interface: {e}");
                }

                return Json(stats);
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public async Task<IActionResult> AddToQueue(ulong channelId, string clientSecret, string id, bool next = false,
            bool spotify = false)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                var user = WebUiUsers.Get(clientSecret);
                if (user == null) return Ok("403");

                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                PlayableItem search;

                if (!spotify) search = await Video.SearchById(id);
                else search = await Track.Get(id, true);
                if (search == null) return Ok("410");
                var req = player.CurrentGuild?.Members[user.Id];
                search.SetRequester(req);
                if (!next) player.Queue.AddToQueue(search);
                else player.Queue.AddToQueueNext(search);
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public IActionResult GetQueue(ulong channelId)
        {
            if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
            var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
            var queue = player.Queue.Items;
            var items = queue.Select(qu => new SearchResult
            {
                Title = qu.GetTitle(),
                Author = qu.GetAuthor(),
                //Index = queue.IndexOf(qu),
                ThumbnailUrl = qu.GetThumbnailUrl() ?? "nothumb.png",
                Length =
                    qu.GetLength() == 0 ? "10" : TimeSpan.FromMilliseconds(qu.GetLength()).ToString("hh\\:mm\\:ss"),
                Url = qu.GetType() == typeof(YoutubeVideoInformation)
                    ? $"https://youtube.com/watch?v={qu.GetId()}"
                    : "no"
                //VoiceUsers = player.VoiceUsers.Count
            }).ToList();
            return Json(items);
        }

        public IActionResult Pause(ulong channelId, string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                player.Pause();
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public async Task<IActionResult> Skip(ulong channelId, string clientSecret, int times = 1)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                await player.Skip(times);
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public IActionResult GoTo(ulong channelId, string clientSecret, int index = -555)
        {
            try
            {
                if (index == -555) return Ok("404");
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                player.GoToIndex(index);
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public IActionResult Shuffle(ulong channelId, string clientSecret, int seed = -555)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                player.Queue.Shuffle();
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public IActionResult Leave(ulong channelId, string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                player.Disconnect();
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public async Task<IActionResult> RemoveFromQueue(ulong channelId, string clientSecret, int index)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return Ok("404");
                if (Manager.Main.All(ch => ch.VoiceChannel?.Id != channelId)) return Ok("403");
                var player = Manager.Main.First(ch => ch.VoiceChannel?.Id == channelId);
                if (index == player.Queue.Current)
                {
                    player.Queue.RemoveFromQueue(index);
                    player.Queue.Current -= 1;
                    await player.Skip();
                    return Ok("200");
                }

                if (index < player.Queue.Current)
                {
                    player.Queue.RemoveFromQueue(index);
                    player.Queue.Current -= 1;
                    return Ok("200");
                }

                player.Queue.RemoveFromQueue(index);
                return Ok("200");
            }
            catch (Exception)
            {
                return Ok("503");
            }
        }

        public string CheckInformation(string clientSecret)
        {
            return !WebUiUsers.ContainsValue(clientSecret) ? "404" : "200";
        } // ReSharper disable UnusedAutoPropertyAccessor.Local

        private struct GuildItem
        {
            public string Name { get; init; }
            public string Id { get; init; }

            public string IconUrl { get; init; }
        }

        private struct UserInfo
        {
            public string Username { get; init; }
            public string Discriminator { get; init; }
            public string ImageUrl { get; init; }
        }
    }
}