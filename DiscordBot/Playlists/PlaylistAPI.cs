#nullable enable
using System;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace DiscordBot.Playlists
{
    public class PlaylistAPI : Controller
    {
        [HttpGet]
        public ActionResult Index(string? list)
        {
            if (!Guid.TryParse(list, out var guid)) return BadRequest();

            var playlist = PlaylistManager.GetIfExists(guid);
            
            if (playlist is null) 
                return NotFound(PlaylistPageGenerator.GenerateNotFoundPage());

            return Ok(PlaylistPageGenerator.GenerateNormalPage(playlist.Value));
        }

        [HttpGet]
        public async Task Thumbnail(string? id)
        {
            try
            {
                Response.StatusCode = 200;
                Response.Headers.Add(HeaderNames.ContentDisposition, "filename=image.bmp");
                Response.Headers.Add( HeaderNames.ContentType, "image/bmp");
                var output = Response.Body;
                StreamSpreader? spreader;
                if (!Guid.TryParse(id, out var guid))
                {
                    Response.StatusCode = 404;
                    spreader = PlaylistThumbnail.GetNotFoundImage(output);
                    await (spreader?.Finish() ?? Task.CompletedTask);
                    await Response.CompleteAsync();
                    return;
                }
                var playlist = PlaylistManager.GetIfExists(guid);
                if (playlist?.Info == null)
                {
                    Response.StatusCode = 404;
                    spreader = PlaylistThumbnail.GetNotFoundImage(output);
                    await (spreader?.Finish() ?? Task.CompletedTask);
                    await Response.CompleteAsync();
                    return;
                }

                spreader = PlaylistThumbnail.GetImage(guid.ToString(), playlist.Value.Info, false, output);
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