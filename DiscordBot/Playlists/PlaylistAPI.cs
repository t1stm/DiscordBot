#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Streams;

namespace DiscordBot.Playlists;

public class PlaylistAPI : Controller
{
    [HttpGet]
    [Route("/PlaylistAPI")]
    public ActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("/PlaylistAPI/Editor/{**id}")]
    public ActionResult Editor(string? id)
    {
        return View();
    }

    [HttpGet]
    [Route("/PlaylistAPI/PublicPlaylists")]
    public ActionResult Public()
    {
        var infos = PlaylistManager.GetAll();
        var publicPlaylists = infos.AsParallel().Where(r => r.IsPublic).ToArray().AsReadOnly();
        ViewData.Add("playlists", publicPlaylists);
        return View();
    }

    [HttpGet]
    [Route("/PlaylistAPI/{**id}")]
    public async Task<ContentResult> Playlist(string? id)
    {
        await Debug.WriteAsync($"Playlist ID request is: \"{id}\"");
        if (!Guid.TryParse(id, out var guid))
            return base.Content(PlaylistPageGenerator.GenerateNotFoundPage(), "text/html");

        var playlist = PlaylistManager.GetIfExists(guid);

        return base.Content(
            playlist is null
                ? PlaylistPageGenerator.GenerateNotFoundPage()
                : await PlaylistPageGenerator.GenerateNormalPage(playlist.Value), "text/html");
    }

    [HttpGet]
    [Route("/PlaylistAPI/JSON/{**id}")]
    public async Task<ActionResult> Json(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return BadRequest();
        var playlist = PlaylistManager.GetIfExists(guid);
        return playlist is null
            ? BadRequest()
            : Ok(JsonSerializer.Serialize(new // Anonymous object poggers.
            {
                info = new
                {
                    name = playlist.Value.Info?.Name,
                    maker = playlist.Value.Info?.Maker,
                    description = playlist.Value.Info?.Description
                },
                data = (await PlaylistManager.ParsePlaylist(playlist.Value)).Select(r => r.ToSearchResult())
            }));
    }

    [HttpGet]
    [Route("/PlaylistAPI/Thumbnail/{**id}")]
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
                await (spreader?.FlushAsync()).ExecuteIfNotNull();
                await Response.CompleteAsync();
                return;
            }

            var playlist = PlaylistManager.GetIfExists(guid);
            if (playlist?.Info == null)
            {
                Response.Headers.Add(HeaderNames.ContentDisposition, "filename=not-found.png");
                spreader = await PlaylistThumbnail.GetNotFoundInfo(output);
                await (spreader?.FlushAsync()).ExecuteIfNotNull();
                await Response.CompleteAsync();
                return;
            }

            Response.Headers.Add(HeaderNames.ContentDisposition, $"filename={id}.png");

            spreader = await PlaylistThumbnail.GetImage(guid.ToString(), playlist.Value.Info, false, output);
            await (spreader?.FlushAsync()).ExecuteIfNotNull();

            await Response.CompleteAsync();
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Thumbnail Generator failed: {e}");
            Response.StatusCode = 404;
            await Response.CompleteAsync();
        }
    }


    [HttpGet]
    [Route("/PlaylistAPI/Image/{**id}")]
    public async Task Image(string? id)
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
                spreader = await PlaylistThumbnail.WriteNotFoundPlaylistImage(output);
                await spreader.FlushAsync();
                await Response.CompleteAsync();
                return;
            }

            var playlist = PlaylistManager.GetIfExists(guid);
            if (playlist?.Info == null)
            {
                Response.Headers.Add(HeaderNames.ContentDisposition, "filename=not-found.png");
                spreader = await PlaylistThumbnail.WriteNotFoundPlaylistImage(output);
                await spreader.FlushAsync();
                await Response.CompleteAsync();
                return;
            }

            Response.Headers.Add(HeaderNames.ContentDisposition, $"filename={id}.png");

            spreader = await PlaylistThumbnail.PlaylistImageSpreader(playlist.Value.Info, output);
            await spreader.FlushAsync();
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