using System;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Objects;
using TagLib;

namespace DiscordBot.Audio.Objects
{
    public class SystemFile : PlayableItem
    {
        public bool IsDiscordAttachment { get; init; }
        public ulong Guild { get; set; }
        private bool Checked { get; set; }

        public override string GetThumbnailUrl()
        {
            return "";
        }

        public override string GetLocation()
        {
            return IsDiscordAttachment ? $"{Bot.WorkingDirectory}/dll/Discord Attachments/{Guild}/{Location}" : base.GetLocation();
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

        public override Task Download()
        {
            if (Checked) return Task.CompletedTask;
            Checked = true;
            try
            {
                var info = File.Create(Location);
                Length = (ulong) info.Properties.Duration.TotalMilliseconds +
                         0; //Fixed: 15 Mar 2022 How can I be this dumb.
                var tag = info.GetTag(TagTypes.AudibleMetadata);
                if (tag == null) return Task.CompletedTask;
                Title = tag.Title;
                Author = tag.JoinedPerformers;
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public override string GetId()
        {
            return "";
        }

        public override string GetTypeOf(ILanguage language)
        {
            return IsDiscordAttachment ? language switch
            {
                English => "Discord Attachment",
                Bulgarian => "Discord Прикачване",
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
            } : 
            language switch
            {
                English => "Local File",
                Bulgarian => "Локален Файл",
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
            };
        }
    }
}