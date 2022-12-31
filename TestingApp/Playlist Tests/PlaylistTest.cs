using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Playlists;

namespace TestingApp
{
    public class PlaylistTest
    {
        public void Test()
        {
            Console.WriteLine("Starting PlaylistTest.");
            var notExistant = PlaylistManager.GetIfExists(new Guid());
            Console.WriteLine($"Non existant playlist is: \"{notExistant}\"");
            var demoPlaylist = new List<PlayableItem>();
            for (var i = 0; i < 12; i++)
                demoPlaylist.Add(new YoutubeVideoInformation
                {
                    YoutubeId = Guid.NewGuid().ToString()[..10]
                });

            var saved = PlaylistManager.SavePlaylist(demoPlaylist, new PlaylistInfo
            {
                Name = "Test playlist.",
                Maker = "Anonymous",
                Description = "le epic. le epic. le epic. le epic. le epic. le epic. le epic. "
            });
            var guid = saved?.Info?.Guid ?? Guid.Empty;
            var exists = PlaylistManager.GetIfExists(guid);
            Console.WriteLine(
                $"({exists?.PlaylistItems?.Length}): \"{string.Concat(exists?.PlaylistItems?.Select(r => $"{r.Data},") ?? Array.Empty<string>())}\"");
        }
    }
}