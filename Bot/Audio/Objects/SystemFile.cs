using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Methods;
using TagLib;
using File = TagLib.File;

namespace DiscordBot.Audio.Objects
{
    public class SystemFile : PlayableItem
    {
        public bool IsDiscordAttachment { get; init; }
        public ulong Guild { get; set; }

        public override string GetThumbnailUrl()
        {
            return "";
        }

        public override string GetLocation()
        {
            return IsDiscordAttachment
                ? $"{Bot.WorkingDirectory}/dll/Discord Attachments/{Guild}/{Location}"
                : base.GetLocation();
        }

        public override string GetAddUrl()
        {
            return IsDiscordAttachment switch
            {
                true => $"dis-att://{Guild}-{Location}",
                false => $"file://{Location}"
            };
        }

        public override ulong GetLength()
        {
            return Length == default ? 0 : Length;
        }

        public override Task ProcessInfo()
        {
            if (Processed) return Task.CompletedTask;
            Processed = true;
            try
            {
                var info = File.Create(GetLocation());
                Length = (ulong) info.Properties.Duration.TotalMilliseconds + 0;
                var tag = info.GetTag(TagTypes.AllTags);
                if (tag == null) return Task.CompletedTask;
                Title = string.IsNullOrEmpty(tag.Title) ? Title : tag.Title;
                Author = string.IsNullOrEmpty(tag.JoinedPerformers) ? Author : tag.JoinedPerformers;
            }
            catch
            {
                // Ignored
            }

            return Task.CompletedTask;
        }

        public override async Task<bool> GetAudioData(params Stream[] outputs)
        {
            try
            {
                var file = System.IO.File.OpenRead(GetLocation());
                foreach (var stream in outputs) await file.CopyToAsync(stream);
                return true;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"OnlineFile GetAudioData method failed: \"{e}\"");
                return false;
            }
        }

        public override string GetId()
        {
            return "";
        }
    }
}