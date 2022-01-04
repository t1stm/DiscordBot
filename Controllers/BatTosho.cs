using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms.Spotify;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Enums;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using DSharpPlus;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using Playlist = BatToshoRESTApp.Audio.Platforms.Spotify.Playlist;
using YtPlaylist = BatToshoRESTApp.Audio.Platforms.Youtube.Playlist;


namespace BatToshoRESTApp.Controllers
{
    public class BatTosho : Controller
    {
        public static Dictionary<ulong, string> WebUiUsers = new();

        public static void LoadUsers()
        {
            try
            {
                var file = System.IO.File.ReadAllText($"{Bot.WorkingDirectory}/WebUIUsers.json");
                var dict = JsonSerializer.Deserialize<Dictionary<ulong, string>>(file);
                WebUiUsers = dict ?? new Dictionary<ulong, string>();
                PrintUsers();
            }
            catch (Exception)
            {
                WebUiUsers = new Dictionary<ulong, string>();
                System.IO.File.WriteAllText($"{Bot.WorkingDirectory}/WebUIUsers.json",
                    JsonSerializer.Serialize(WebUiUsers));
            }
        }

        public static void PrintUsers()
        {
            foreach (var (id, secret) in WebUiUsers) Debug.Write($"Id: {id}, Secret: {secret}");
        }

        public static void AddUser(ulong userId, string clientSecret)
        {
            WebUiUsers.Add(userId, clientSecret);
            System.IO.File.WriteAllText($"{Bot.WorkingDirectory}/WebUIUsers.json",
                JsonSerializer.Serialize(WebUiUsers));
        }

        public async Task<string> Search(string searchTerm)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());
            var items = new List<SearchResult>();
            if (searchTerm.Contains("https://open.spotify.com/playlist"))
            {
                var sp = await Playlist.Get(searchTerm.Split("/playlist/").Last().Split("?si")
                    .First());
                items = sp.Select(track => new SearchResult
                {
                    Title = track.Title,
                    Author = track.Author,
                    IsSpotify = true,
                    Length = TimeSpan.FromMilliseconds(track.Length).ToString("hh\\:mm\\:ss"),
                    Id = track.TrackId,
                    ThumbnailUrl = "spotify.png"
                }).ToList();
            }
            else if (searchTerm.Contains("youtu"))
            {
                var yt = new YtPlaylist();
                if (searchTerm.Contains("watch?v="))
                {
                    if (searchTerm.Contains("&list"))
                    {
                        var video = await yt.Get(
                            $"https://youtube.com/playlist?list={searchTerm.Split("list=")[1].Split("&")[0]}");
                        var vid = video.First(vi => vi.GetId() == searchTerm.Split("watch?v=")[1].Split("&")[0]);
                        video.Remove(vid);
                        video.Insert(0, vid);
                        items = video.Select(vi => new SearchResult
                            {
                                Id = vi.GetId(),
                                Author = vi.GetAuthor(),
                                Title = vi.GetTitle(),
                                ThumbnailUrl = vi.GetThumbnailUrl().Split("?")[0],
                                Length = TimeSpan.FromMilliseconds(vi.GetLength()).ToString("hh\\:mm\\:ss"),
                                Url = "https://youtube.com/watch?v=" + vi.GetId()
                            })
                            .ToList();
                    }
                    else
                    {
                        var vi = await new Video().SearchById(searchTerm.Split("watch?v=")[1].Split("&")[0]);
                        items = new List<SearchResult>
                        {
                            new()
                            {
                                Id = vi.GetId(),
                                Author = vi.GetAuthor(),
                                Title = vi.GetTitle(),
                                ThumbnailUrl = vi.GetThumbnailUrl().Split("?")[0],
                                Length = TimeSpan.FromMilliseconds(vi.GetLength()).ToString("hh\\:mm\\:ss"),
                                Url = "https://youtube.com/watch?v=" + vi.GetId()
                            }
                        };
                    }
                }
                else if (searchTerm.Contains("playlist?list"))
                {
                    var video = await yt.Get(searchTerm);
                    items = video.Select(vi => new SearchResult
                        {
                            Id = vi.GetId(),
                            Author = vi.GetAuthor(),
                            Title = vi.GetTitle(),
                            ThumbnailUrl = vi.GetThumbnailUrl().Split("?")[0],
                            Length = TimeSpan.FromMilliseconds(vi.GetLength()).ToString("hh\\:mm\\:ss"),
                            Url = "https://youtube.com/watch?v=" + vi.GetId()
                        })
                        .ToList();
                }
            }
            else
            {
                var video = await client.Search.GetVideosAsync(searchTerm).CollectAsync(25);
                items = video.Select(vid => new SearchResult
                    {
                        Title = vid.Title,
                        Author = vid.Author.Title,
                        IsSpotify = false,
                        Length = vid.Duration?.ToString("hh\\:mm\\:ss"),
                        ThumbnailUrl = vid.Thumbnails[0].Url.Split("?")[0],
                        Url = vid.Url,
                        Id = vid.Id.Value
                    })
                    .ToList();
            }

            var jsonRes = JsonSerializer.Serialize(items);
            return jsonRes;
        }

        public string GetAvailableGuilds(string clientSecret)
        {
            if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
            var userIndex = -1;
            try
            {
                userIndex = WebUiUsers.Values.ToList().IndexOf(clientSecret);
            }
            catch (Exception)
            {
                return "403";
            }

            try
            {
                if (userIndex == -1) return "418 I'm a teapot";
                var guilds = Bot.Clients[0].Guilds.Values
                    .Where(gi => gi.Members.ContainsKey(WebUiUsers.Keys.ElementAt(userIndex)));
                var items = guilds.Select(g => new GuildItem {Id = g.Id + "", Name = g.Name, IconUrl = g.IconUrl})
                    .ToList();
                return JsonSerializer.Serialize(items);
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public async Task<string> GetChannelsInGuild(ulong id, string clientSecret)
        {
            if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
            var userIndex = -1;
            try
            {
                userIndex = WebUiUsers.Values.ToList().IndexOf(clientSecret);
            }
            catch (Exception)
            {
                return "403";
            }

            try
            {
                if (userIndex == -1) return "418 I'm a teapot";
                var guild = Bot.Clients[0].Guilds.First(g => g.Value.Id == id).Value;
                var channels = await guild.GetChannelsAsync();
                var items = channels.Where(ch => ch.Type == ChannelType.Voice).Where(ch => Manager.Main.ContainsKey(ch))
                    .Select(g => new GuildItem {Id = g.Id + "", Name = g.Name}).ToList();
                return JsonSerializer.Serialize(items);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in GetChannelsInGuild: {e}");
                return "503";
            }
        }

        public async Task<string> GetUserStats(string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                var userIndex = -1;
                try
                {
                    userIndex = WebUiUsers.Values.ToList().IndexOf(clientSecret);
                }
                catch (Exception)
                {
                    return "403";
                }

                var user = await Bot.Clients[0].GetUserAsync(WebUiUsers.Keys.ToList()[userIndex], true);
                var info = new UserInfo
                {
                    Username = user.Username,
                    Discriminator = user.Discriminator,
                    ImageUrl = user.GetAvatarUrl(ImageFormat.Auto)
                };
                return JsonSerializer.Serialize(info);
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string GetPlayerStats(ulong channelId)
        {
            try
            {
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                var stats = new PlayerInfo();
                try
                {
                    stats.Title = player.CurrentItem.GetTitle();
                    stats.Author = player.CurrentItem.GetAuthor();
                    stats.Current = player.Stopwatch.Elapsed.ToString("hh\\:mm\\:ss");
                    stats.Total = TimeSpan.FromMilliseconds(player.CurrentItem.GetLength())
                        .ToString("hh\\:mm\\:ss");
                    stats.TotalDuration = player.CurrentItem.GetLength();
                    stats.CurrentDuration = (ulong) player.Stopwatch.ElapsedMilliseconds;
                    stats.Loop = player.LoopStatus switch
                    {
                        Loop.None => "None", Loop.One => "One", Loop.WholeQueue => "WholeQueue",
                        _ => "bad"
                    };
                    stats.ThumbnailUrl = player.CurrentItem.GetThumbnailUrl();
                    stats.Paused = player.Paused;
                    stats.Index = player.Queue.Items.ToList().IndexOf(player.CurrentItem);
                }
                catch (Exception e)
                {
                    Debug.Write($"Error in generating current song information for web interface: {e}");
                }

                return JsonSerializer.Serialize(stats);
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public async Task<string> AddToQueue(ulong channelId, string clientSecret, string id, bool next = false,
            bool spotify = false)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var userIndex = -1;
                try
                {
                    userIndex = WebUiUsers.Values.ToList().IndexOf(clientSecret);
                }
                catch (Exception)
                {
                    return "403";
                }

                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                IPlayableItem search;

                if (!spotify) search = await new Video().SearchById(id);
                else search = await Track.Get(id, true);

                if (search == null) return "410";
                var req = player.CurrentGuild.Members[WebUiUsers.Keys.ToList()[userIndex]];
                search.SetRequester(req);
                if (!next)
                    player.Queue.AddToQueue(search);
                else
                    player.Queue.AddToQueueNext(search);
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string GetQueue(ulong channelId)
        {
            if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
            var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
            var queue = player.Queue.Items;
            var items = queue.Select(qu => new SearchResult
            {
                Title = qu.GetTitle(),
                Author = qu.GetAuthor(),
                Index = queue.IndexOf(qu),
                ThumbnailUrl = qu.GetThumbnailUrl() ?? "nothumb.png",
                Length =
                    qu.GetLength() == 0 ? "10" : TimeSpan.FromMilliseconds(qu.GetLength()).ToString("hh\\:mm\\:ss"),
                Url = qu.GetType() == typeof(YoutubeVideoInformation)
                    ? $"https://youtube.com/watch?v={qu.GetId()}"
                    : "no",
                VoiceUsers = player.VoiceUsers
            }).ToList();
            var jsonRes = JsonSerializer.Serialize(items);
            return jsonRes;
        }

        public string Pause(ulong channelId, string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                player?.Pause();
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public async Task<string> Skip(ulong channelId, string clientSecret, int times = 1)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                await player.Skip(times);
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string GoTo(ulong channelId, string clientSecret, int index = -555)
        {
            try
            {
                if (index == -555) return "404";
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                player.GoToIndex(index);
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string Shuffle(ulong channelId, string clientSecret, int seed = -555)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                player.Queue.Shuffle();
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string Leave(ulong channelId, string clientSecret)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                player.Disconnect();
                Manager.Main.Remove(Manager.Main.Keys.First(ch => ch.Id == channelId));
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public async Task<string> RemoveFromQueue(ulong channelId, string clientSecret, int index)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                if (index == player.Queue.Current)
                {
                    player.Queue.RemoveFromQueue(index);
                    player.Queue.Current -= 1;
                    await player.Skip();
                    return "200";
                }

                if (index < player.Queue.Current)
                {
                    player.Queue.RemoveFromQueue(index);
                    player.Queue.Current -= 1;
                    return "200";
                }

                player.Queue.RemoveFromQueue(index);
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }

        public string GetChat(string clientSecret, ulong channelId)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                var fDir = $"{Bot.WorkingDirectory}/chat/{channelId}.chat";
                if (!System.IO.File.Exists(fDir)) System.IO.File.WriteAllText(fDir, "{}");
                var els = JsonSerializer.Deserialize<List<ChatMessage>>(System.IO.File.ReadAllText(fDir));
                return JsonSerializer.Serialize(els);
            }
            catch (Exception e)
            {
                Debug.Write($"Error in getting chat messages. {e}");
                return "503";
            }
        }

        public async Task<string> AddToChat(string clientSecret, ulong channelId, string message)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                var fDir = $"{Bot.WorkingDirectory}/chat/{channelId}.chat";
                if (!System.IO.File.Exists(fDir)) await System.IO.File.WriteAllTextAsync(fDir, "{}");
                var els = JsonSerializer.Deserialize<List<ChatMessage>>(await System.IO.File.ReadAllTextAsync(fDir)) ??
                          new List<ChatMessage>();
                var userIndex = -1;
                try
                {
                    userIndex = WebUiUsers.Values.ToList().IndexOf(clientSecret);
                }
                catch (Exception)
                {
                    return "403";
                }

                var user = await Bot.Clients[0].GetUserAsync(WebUiUsers.Keys.ElementAt(userIndex));
                els.Add(new ChatMessage
                {
                    Username = $"{user.Username}{user.Discriminator}",
                    Date = DateTime.Now.ToShortDateString(),
                    Message = message
                });
                return JsonSerializer.Serialize(els);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Error in getting chat messages. {e}");
                return "503";
            }
        }

        /*public string RemoveFromQueue(ulong channelId, string clientSecret, string term)
        {
            try
            {
                if (!WebUiUsers.ContainsValue(clientSecret)) return "404";
                if (Manager.Main.Keys.All(ch => ch.Id != channelId)) return "403";
                var player = Manager.Main[Manager.Main.Keys.First(ch => ch.Id == channelId)];
                player.Queue.RemoveFromQueue(term);
                return "200";
            }
            catch (Exception)
            {
                return "503";
            }
        }*/

        public string CheckInformation(string clientSecret)
        {
            return !WebUiUsers.ContainsValue(clientSecret) ? "404" : "200";
        } // ReSharper disable UnusedAutoPropertyAccessor.Local
        private struct SearchResult
        {
            public string Title { get; init; }
            public string Author { get; init; }
            public string Length { get; init; }
            public string Url { get; init; }
            public string ThumbnailUrl { get; init; }
            public bool IsSpotify { get; init; }

            public long Index { get; init; }
            public int VoiceUsers { get; init; }
            public string Id { get; init; }
        }

        private struct GuildItem
        {
            public string Name { get; init; }
            public string Id { get; init; }

            public string IconUrl { get; init; }
        }

        private struct PlayerInfo
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public ulong CurrentDuration { get; set; }
            public string Current { get; set; }
            public ulong TotalDuration { get; set; }
            public string Total { get; set; }
            public string Loop { get; set; }
            public string ThumbnailUrl { get; set; }
            public bool Paused { get; set; }

            public long Index { get; set; }
        }

        private struct UserInfo
        {
            public string Username { get; init; }
            public string Discriminator { get; init; }
            public string ImageUrl { get; init; }
        }

        private struct ChatMessage
        {
            public string Date { get; init; }
            public string Username { get; init; }
            public string Message { get; init; }
        }
    }
}