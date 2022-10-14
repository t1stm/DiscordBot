#nullable enable
using System;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace DiscordBot.Playlists
{
    public class PlaylistAPI : Controller
    {
        
        [HttpGet,Route("/PlaylistAPI/Playlist/{**id}")]
        public async Task<ContentResult> Playlist(string? id)
        {
            await Debug.WriteAsync($"Playlist ID is: \"{id}\"");
            if (!Guid.TryParse(id, out var guid))
                return base.Content(PlaylistPageGenerator.GenerateNotFoundPage(), "text/html");

            var playlist = PlaylistManager.GetIfExists(guid);
            
            return base.Content(playlist is null ? 
                PlaylistPageGenerator.GenerateNotFoundPage() : 
                await PlaylistPageGenerator.GenerateNormalPage(playlist.Value), "text/html");
        }

        [HttpGet,Route("/PlaylistAPI/Thumbnail/{**id}")]
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
                    spreader = await PlaylistThumbnail.GetNotFoundImage(output);
                    await (spreader?.Finish() ?? Task.CompletedTask);
                    await Response.CompleteAsync();
                    return;
                }
                var playlist = PlaylistManager.GetIfExists(guid);
                if (playlist?.Info == null)
                {
                    Response.Headers.Add(HeaderNames.ContentDisposition, "filename=not-found.png");
                    spreader = await PlaylistThumbnail.GetNotFoundImage(output);
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