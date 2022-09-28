#nullable enable
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Playlists
{
    public class PlaylistAPI : Controller
    {
        [HttpGet]
        public IActionResult? Index(string? list)
        {
            return null;
        }
    }
}