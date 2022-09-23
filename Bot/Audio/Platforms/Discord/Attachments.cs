#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Readers;
using DSharpPlus.Entities;

namespace DiscordBot.Audio.Platforms.Discord
{
    public static class Attachments
    {
        private const string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/Discord Attachments/";

        public static async Task<List<PlayableItem?>> GetAttachments(List<DiscordAttachment> attachments,
            ulong guild = 0)
        {
            var list = new List<PlayableItem?>();
            foreach (var at in attachments)
                if (at.FileName.EndsWith(".batp"))
                {
                    list.AddRange(await SharePlaylist.Get(at) ?? throw new InvalidOperationException());
                }
                else if (at.FileName.EndsWith(".txt"))
                {
                    var stream = await HttpClient.DownloadStream(at.Url);
                    var text = Encoding.UTF8.GetString(stream.GetBuffer());
                    list.Add(new TtsText(text));
                }
                else
                {
                    list.Add(await GetAttachment(at, guild));
                }

            return list;
        }

        private static async Task<PlayableItem> GetAttachment(DiscordAttachment attachment, ulong guild = 0)
        {
            var url = attachment.Url;
            var location = DownloadDirectory +
                           guild switch {0 => $"noId/{attachment.FileName}", _ => $"{guild}/{attachment.FileName}"};
            if (!Directory.Exists(DownloadDirectory + $"{guild}/"))
                Directory.CreateDirectory(DownloadDirectory + $"{guild}/");
            if (File.Exists(location)) File.Delete(location);
            await HttpClient.DownloadFile(url, location);
            var file = new SystemFile
            {
                Title = attachment.FileName,
                Author = "",
                Length = 0,
                Location = attachment.FileName,
                IsDiscordAttachment = true,
                Guild = guild
            };
            return file;
        }
    }
}