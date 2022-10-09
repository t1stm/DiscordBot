#nullable enable
using System;
using System.Threading.Tasks;
using DiscordBot.Methods;
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
                Response.Headers.Add(HeaderNames.ContentDisposition, "filename=image.png");
                Response.Headers.Add( HeaderNames.ContentType, "image/png");
                var output = Response.Body;
                
                if (!Guid.TryParse(id, out var guid))
                {
                    PlaylistThumbnail.GetNotFoundImage(output);
                    Response.StatusCode = 404;
                    return;
                }
                var playlist = PlaylistManager.GetIfExists(guid);
                if (playlist?.Info == null)
                {
                    PlaylistThumbnail.GetNotFoundImage(output);
                    Response.StatusCode = 404;
                    return;
                }

                PlaylistThumbnail.GetImage(guid.ToString(), playlist.Value.Info, false, output);

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