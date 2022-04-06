using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Audio.Platforms.Discord;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using Playlist = BatToshoRESTApp.Audio.Platforms.Spotify.Playlist;
using YtPlaylist = BatToshoRESTApp.Audio.Platforms.Youtube.Playlist;

namespace BatToshoRESTApp.Standalone
{
    public class Audio : Controller
    {
        public async Task<FileStreamResult> DownloadTrack(string id, bool getRaw = true, int bitrate = 96,
            bool useOpus = true)
        {
            await Debug.WriteAsync("Using Audio Controller");
            FfMpeg2 ff = new();
            var file = $"{Bot.WorkingDirectory}/dll/audio/{id}.webm";
            if (System.IO.File.Exists(file))
                return File((!getRaw || !useOpus) switch
                {
                    true => ff.Convert(file, "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis", $"-b:a {bitrate}k"),
                    false => ff.Convert(file)
                }, "audio/ogg", "audio.ogg", true);
            var search = new Search();
            var res = await search.Get(id.StartsWith("http") ? id : $"https://youtube.com/watch?v={id}");
            file = res[0].GetLocation();
            return File((!getRaw || !useOpus) switch
            {
                true => ff.Convert(file, "-f ogg", useOpus ? "-c:a libopus" : "-c:a libvorbis", $"-b:a {bitrate}k"),
                false => ff.Convert(file)
            }, "audio/ogg", "audio.ogg", true);
        }

        public async Task<JsonResult> Search(string term)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());
            var items = new List<BatTosho.SearchResult>();
            if (term.StartsWith("pl:"))
            {
                var shp = SharePlaylist.Get(term[3..]);
                items = shp.Select(item => new BatTosho.SearchResult
                {
                    Title = item.GetTitle(),
                    Author = item.GetAuthor(),
                    IsSpotify = false,
                    Length = TimeSpan.FromMilliseconds(item.GetLength()).ToString("hh\\:mm\\:ss"),
                    Id = item.GetId(),
                    ThumbnailUrl = item.GetThumbnailUrl()
                }).ToList();
                return Json(items);
            }

            if (term.Contains("https://open.spotify.com/playlist"))
            {
                var sp = await Playlist.Get(term.Split("/playlist/").Last().Split("?si")
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
            else if (term.Contains("youtu"))
            {
                var yt = new YtPlaylist();
                if (term.Contains("watch?v="))
                {
                    if (term.Contains("&list"))
                    {
                        var video = await yt.Get(
                            $"https://youtube.com/playlist?list={term.Split("list=")[1].Split("&")[0]}");
                        var vid = video.First(vi => vi.GetId() == term.Split("watch?v=")[1].Split("&")[0]);
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
                        var vi = await new Video().SearchById(term.Split("watch?v=")[1].Split("&")[0]);
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
                else if (term.Contains("playlist?list"))
                {
                    var video = await yt.Get(term);
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
                var video = await client.Search.GetVideosAsync(term).CollectAsync(25);
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

            return Json(items.ToList());
        }

        public FileStreamResult GetRandomDownload()
        {
            var files = Directory.EnumerateFiles($"{Bot.WorkingDirectory}/dll/audio").ToList();
            var rng = new Random();
            var rnFile = rng.Next(files.Count);
            FfMpeg2 ff = new();
            return File(ff.Convert(files[rnFile]), "audio/ogg", "random.ogg");
        }
    }
}