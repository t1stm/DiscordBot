using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Enums;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using WebSocketSharper;
using WebSocketSharper.Server;
using YoutubeExplode;
using YoutubeExplode.Common;
using Playlist = BatToshoRESTApp.Audio.Platforms.Spotify.Playlist;
using YtPlaylist = BatToshoRESTApp.Audio.Platforms.Youtube.Playlist;

namespace BatToshoRESTApp.Controllers.Objects
{
    public class WebSock : WebSocketBehavior
    {
        private Player _currentPlayer;

        private long _clients;

        private List<IPlayableItem> _oldItems = new();
        private IPlayableItem _item = null;
        
        public WebSock(Player pl)
        {
            _currentPlayer = pl;
            Debug.Write("Adding WebSocket Channel");
            var task = new Task(async () => await Checker());
            task.Start();
        }

        private async Task Checker()
        {
            while (!_currentPlayer.Die)
            {
                await Task.Delay(333);
                if (_clients < 1) continue;
                Sessions.Broadcast($"Time:{_currentPlayer.Stopwatch.ElapsedMilliseconds}/{_currentPlayer.CurrentItem.GetLength()}");
                if (_item != _currentPlayer.CurrentItem)
                {
                    _item = _currentPlayer.CurrentItem;
                    Sessions.Broadcast($"Item:{GetStats()}");
                }
                if (_oldItems == _currentPlayer.Queue.Items) continue;
                _oldItems = _currentPlayer.Queue.Items;
                Sessions.Broadcast($"Queue:{GetQueueJson()}");
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Write($"WebSocket Connection Closed: {e.Reason} {e.Code}");
            _clients--;
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.Write($"WebSocket Connection Errored: {e.Exception.Message}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Debug.Write($"WebSocket Connection Message: {e.Data}");
            try
            {
                if (e.IsBinary||e.IsPing) return;
                string data = e.Data;
                string[] arr = data.Split(":");
                if (arr.Length != 2)
                {
                    Debug.Write("WebSocket Connection: Invalid Format");
                    return;
                }

                if (!BatTosho.WebUiUsers.ContainsValue(arr[0]))
                {
                    Debug.Write("WebSocket Connection: Not a WebUi User");
                    return;
                }

                var task = new Task(async () => await Task.CompletedTask);
                string message = arr[1];
                if (message == "GetQueue")
                {
                    Send($"Queue:{GetQueueJson()}");
                }
                else switch (message[..5])
                {
                    case "Skip=" when int.TryParse(message[5..], out int val):
                        _currentPlayer.Skip(val).Wait();
                        break;
                    case "Skip=":
                        _currentPlayer.Skip().Wait();
                        break;
                    case "Play=":
                        task = new Task(async () =>
                        {
                            var list = await new Search().Get(message[5..]);
                            _currentPlayer.Queue.AddToQueue(list);
                        });
                        break;
                    case "Leave":
                        _currentPlayer.Disconnect();
                        break;
                    case "Pause":
                        _currentPlayer.Pause();
                        break;
                    case "Stats":
                        task = new Task(() =>
                        {
                            Send($"Stats:{GetStats()}");
                        });
                        break;
                    default:
                    {
                        if (message[..9] == "PlayNext=")
                        {
                            task = new Task(async () =>
                            {
                                var list = await new Search().Get(message[9..]);
                                _currentPlayer.Queue.AddToQueueNext(list);
                            });
                        }
                        else switch (message[..7])
                        {
                            case "Search=":
                                task = new Task(async () =>
                                {
                                    Send(await Search(message[..7]));
                                });
                                break;
                            case "Shuffle":
                                _currentPlayer.Shuffle();
                                break;
                        }
                        break;
                    }
                }
                task.Start();
            }
            catch (Exception ex)
            {
                Debug.Write($"Exception on message receive: {ex}");
            }
        }

        private string GetStats()
        {
            var player = _currentPlayer;
            var stats = new BatTosho.PlayerInfo();
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
                Debug.Write($"Error in generating current song information for Web Sockets: {e}");
            }

            return JsonSerializer.Serialize(stats);
        }

        protected override void OnOpen()
        {
            Debug.Write("WebSocket Connection Opened");
            _clients++;
        }

        private string GetQueueJson()
        {
            var queue = _currentPlayer.Queue.Items; 
            var items = queue.Select(qu => new BatTosho.SearchResult
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
                VoiceUsers = _currentPlayer.VoiceUsers
            }).ToList();
            return JsonSerializer.Serialize(items);
        }

        private async Task<string> Search(string searchTerm)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());
            var items = new List<BatTosho.SearchResult>();
            if (searchTerm.Contains("https://open.spotify.com/playlist"))
            {
                var sp = await Playlist.Get(searchTerm.Split("/playlist/").Last().Split("?si")
                    .First());
                items = sp.Select(track => new BatTosho.SearchResult
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
                        items = video.Select(vi => new BatTosho.SearchResult
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
                        items = new List<BatTosho.SearchResult>
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
                    items = video.Select(vi => new BatTosho.SearchResult
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
                items = video.Select(vid => new BatTosho.SearchResult
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

            return JsonSerializer.Serialize(items);
        }
    }
}