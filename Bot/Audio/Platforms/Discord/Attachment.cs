using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;

namespace Bat_Tosho.Audio.Platforms.Discord
{
    public static class Attachment
    {
        public static async Task<VideoInformation> Download(DiscordAttachment attachment, DiscordUser user,
            ulong serverId)
        {
            string name = "", author = "";
            var length = 0;
            var url = attachment.Url;
            var filepath = $"{Program.MainDirectory}/dll/DiscordAttachments/{serverId}/{attachment.FileName}";

            if (!Directory.Exists($"{Program.MainDirectory}/dll/DiscordAttachments/{serverId}"))
                Directory.CreateDirectory($"{Program.MainDirectory}/dll/DiscordAttachments/{serverId}");

            var genAuthor = attachment.FileName.Split("-").First().Replace("_", " ").Trim();
            var genTitle = attachment.FileName.Split("-").Last().Split(".").First().Replace("_", " ").Trim();
            switch (File.Exists(filepath))
            {
                case false:
                    using (WebClient www = new())
                    {
                        await Debug.Write($"Downloading Discord Attachment: {url} to {filepath}");
                        await www.DownloadFileTaskAsync(url, filepath);
                    }

                    break;
            }

            try
            {
                var tagFile = TagLib.File.Create(filepath);
                name = string.IsNullOrEmpty(tagFile.Tag.Title) switch {true => genTitle, _ => tagFile.Tag.Title};
                author = string.IsNullOrEmpty(tagFile.Tag.JoinedPerformers) switch
                {
                    true => genAuthor, _ => tagFile.Tag.JoinedPerformers
                };
                length = (int) tagFile.Properties.Duration.TotalMilliseconds;
            }
            catch (Exception e)
            {
                await Debug.Write($"Error reading tags: {e.Message}");
                name = genTitle;
                author = genAuthor;
                length = -1;
            }

            return new VideoInformation(filepath, VideoSearchTypes.Downloaded, PartOf.DiscordAttachment, name, author,
                length, user);
        }
    }
}