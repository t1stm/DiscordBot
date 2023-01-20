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
            var demoPlaylist = new List<PlayableItem>
            {
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                },
                new YoutubeVideoInformation
                {
                    YoutubeId = "OjNpRbNdR7E"
                }
            };


            var saved = PlaylistManager.SavePlaylist(demoPlaylist, new PlaylistInfo
            {
                Name = "Почит за Мао Дзъдун от Малашевци",
                Maker = "Kristian Gergov",
                Description = "Жив и здрав да е. Снимката му добре да си я пази."
            });
            var guid = saved?.Info?.Guid ?? Guid.Empty;
            var exists = PlaylistManager.GetIfExists(guid);
            Console.WriteLine(
                $"({exists?.PlaylistItems?.Length}): \"{string.Concat(exists?.PlaylistItems?.Select(r => $"{r.Data},") ?? Array.Empty<string>())}\"");
        }
    }
}