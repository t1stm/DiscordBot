using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Methods;
using DiscordBot.Playlists;
using DiscordBot.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace TestingAPI.Controllers
{
    [ApiController]
    public class Test : Controller
    {
        [Route("/Test/TestAction")]
        public ActionResult TestAction()
        {
            return Ok("Hello World!");
        }
        
        [HttpGet, Route("/Audio/Search")]
        public async Task<IActionResult> Search(string term)
        {
            var items = await DiscordBot.Audio.Platforms.Search.Get(term, returnAllResults: true);
            if (items != Status.OK) return Ok("[]"); // Empty Array
            var list = new List<SearchResult>();
            foreach (var item in items.GetOK())
            {
                if (item is not SpotifyTrack track)
                {
                    list.Add(item.ToSearchResult());
                    continue;
                }

                var spotify = await Video.Search(track);
                if (spotify != Status.OK) continue;
                var res = spotify.GetOK().ToSearchResult();
                res.IsSpotify = false;
                list.Add(res);
            }

            return Json(list, new JsonSerializerOptions {PropertyNameCaseInsensitive = false});
        }

        [HttpGet]
        [Route("/Playlist/{**id}")]
        public async Task<ContentResult> Playlist(string? id)
        {
            await Debug.WriteAsync($"Playlist ID is: \"{id}\"");
            if (!Guid.TryParse(id, out var guid))
                return base.Content(PlaylistPageGenerator.GenerateNotFoundPage(), "text/html");

            var playlist = PlaylistManager.GetIfExists(guid);

            return base.Content(
                playlist is null
                    ? PlaylistPageGenerator.GenerateNotFoundPage()
                    : await PlaylistPageGenerator.GenerateNormalPage(playlist.Value), "text/html");
        }

        [HttpGet]
        [Route("/Playlist/Thumbnails/{**id}")]
        public async Task Thumbnail(string? id)
        {
            try
            {
                Response.StatusCode = 200;
                Response.Headers.Add(HeaderNames.ContentType, "image/png");
                var output = Response.Body;
                StreamSpreader? spreader;
                if (!Guid.TryParse(id, out var guid))
                {
                    Response.Headers.Add(HeaderNames.ContentDisposition, "filename=not-found.png");
                    spreader = await PlaylistThumbnail.GetNotFoundInfo(output);
                    await (spreader?.Finish() ?? Task.CompletedTask);
                    await Response.CompleteAsync();
                    return;
                }

                var playlist = PlaylistManager.GetIfExists(guid);
                if (playlist?.Info == null)
                {
                    Response.Headers.Add(HeaderNames.ContentDisposition, "filename=not-found.png");
                    spreader = await PlaylistThumbnail.GetNotFoundInfo(output);
                    await (spreader?.Finish() ?? Task.CompletedTask);
                    await Response.CompleteAsync();
                    return;
                }

                Response.Headers.Add(HeaderNames.ContentDisposition, $"filename={id}.png");

                spreader = await PlaylistThumbnail.GetImage(guid.ToString(), playlist.Value.Info, false, output);
                await (spreader?.Finish() ?? Task.CompletedTask);

                await Response.CompleteAsync();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Thumbnail Generator failed: {e}");
                Response.StatusCode = 404;
                await Response.CompleteAsync();
            }
        }
    }
}