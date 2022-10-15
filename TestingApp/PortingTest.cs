using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CustomPlaylistFormat;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Audio.Platforms;
using DiscordBot.Methods;
using DiscordBot.Playlists;

namespace TestingApp
{
    public class PortingTest
    {
        public void Test()
        {
            var oldPlaylists = Directory.GetDirectories("/nvme0/DiscordBot/Playlists");
            foreach (var playlist in oldPlaylists)
            {
                var channels = Directory.GetDirectories(playlist);
                foreach (var channelDir in channels)
                {
                    ProcessFolder(channelDir);
                }
            }
        }

        private async void ProcessFolder(string directory)
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                await DecodeFile(file);
            }
        }

        public async Task ProcessMainFolder()
        {
            var files = Directory.GetFiles("/nvme0/DiscordBot/Playlists").ToArray();
            await Debug.WriteAsync($"Files count is: {files.Length}");
            for (var index = 0; index < files.Length; index++)
            {
                try
                {
                    var file = files[index];
                    await Debug.WriteAsync($"({index} - {files.Length}) Checking file: \"{file}\"");
                    var token = file.Split('/')[^1];
                    if (!token.Contains('-')) continue;
                    await DecodeFile(token);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Exception in for loop: \"{e}\"");
                    throw;
                }
            }
        }

        
        private async Task DecodeFile(string file)
        {
            // /nvme0/DiscordBot/Playlists
            var split = file.Split('-');
            var items = await SharePlaylist.Get($"{split[^3]}-{split[^2]}-{split[^1][..^5]}");
            if (items == null) return;
            await WriteToNewFile(split[^3], split[^2], split[^1][..^5], items);
        }

        private async Task WriteToNewFile(string guild, string channel, string token, IEnumerable<PlayableItem> items)
        {
            var guid = Guid.NewGuid();
            var path = $"{PlaylistManager.PlaylistDirectory}/{guid}.play";
            await using var file = File.Open(path, FileMode.Create);
            await Debug.WriteAsync($"Creating new playlist: \"{path}\", from old playlist: \"{guild}/{channel}/{token}\"");
            var playlistInfo = new PlaylistInfo
            {
                Name = $"Port:{token}",
                Description = $"A playlist made before the new playlist format.\nGuild: {guild}\nChannel: {channel}",
                Maker = "Anonymous",
                IsPublic = false,
                LastModified = DateTime.UtcNow.Ticks
            };
            var encoder = new Encoder(file, playlistInfo);
            var entries = items.ToList().Select(item => new Entry
            {
                Type = PlaylistManager.GetItemType(item),
                Data = string.Join("://", item.GetAddUrl().Split("://")[1..])
            });
            
            encoder.Encode(entries);
            await file.FlushAsync();
        }
        
        public async Task WriteToNewFileWithInfo(string name, string description, string maker, bool isPublic, IEnumerable<PlayableItem> items)
        {
            var guid = Guid.NewGuid();
            var path = $"{PlaylistManager.PlaylistDirectory}/{guid}.play";
            await using var file = File.Open(path, FileMode.Create);
            await Debug.WriteAsync($"Creating new playlist: \"{path}\"");
            var playlistInfo = new PlaylistInfo
            {
                Name = name,
                Description = description,
                Maker = maker,
                IsPublic = isPublic,
                LastModified = DateTime.UtcNow.Ticks
            };
            var encoder = new Encoder(file, playlistInfo);
            var entries = items.ToList().Select(item => new Entry
            {
                Type = PlaylistManager.GetItemType(item),
                Data = string.Join("://", item.GetAddUrl().Split("://")[1..])
            });
            
            encoder.Encode(entries);
            await file.FlushAsync();
        }
    }
}