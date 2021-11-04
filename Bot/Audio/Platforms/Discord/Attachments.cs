using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Platforms.Discord
{
    public static class Attachments
    {
        private static readonly string DownloadDirectory = $"{Bot.WorkingDirectory}/dll/Discord Attachments/";

        public static async Task<List<SystemFile>> GetAttachments(List<DiscordAttachment> attachments, ulong guild = 0)
        {
            var list = new List<SystemFile>();
            foreach (var at in attachments) list.Add(await GetAttachment(at, guild));
            return list;
        }

        public static async Task<SystemFile> GetAttachment(DiscordAttachment attachment, ulong guild = 0)
        {
            var url = attachment.Url;
            var location = DownloadDirectory +
                           guild switch {0 => $"noId/{attachment.FileName}", _ => $"{guild}/{attachment.FileName}"};
            if (!Directory.Exists(DownloadDirectory + $"{guild}/"))
                Directory.CreateDirectory(DownloadDirectory + $"{guild}/");
            if (File.Exists(location)) File.Delete(location);
            using var wb = new WebClient();
            await wb.DownloadFileTaskAsync(url, location);
            var tags = TagLib.File.Create(location);
            SystemFile file;
            try
            {
                file = new SystemFile
                {
                    Title = tags.Tag.Title,
                    Author = tags.Tag.JoinedPerformers,
                    Length = (ulong) tags.Properties.Duration.TotalMilliseconds,
                    Location = location,
                    IsDiscordAttachment = true
                };
            }
            catch (Exception)
            {
                file = new SystemFile
                {
                    Title = attachment.FileName,
                    Author = "",
                    Length = 0,
                    Location = location,
                    IsDiscordAttachment = true
                };
            }

            return file;
        }
    }
}