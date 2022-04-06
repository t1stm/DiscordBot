using System;
using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;
using TagLib;

namespace BatToshoRESTApp.Audio.Objects
{
    public class SystemFile : PlayableItem
    {
        public bool IsDiscordAttachment { get; init; } = true;
        private bool Checked { get; set; }

        public override string GetThumbnailUrl()
        {
            return "";
        }

        public new ulong GetLength()
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

        public override string GetTypeOf()
        {
            return IsDiscordAttachment ? "Discord Attachment" : "Local File";
        }
    }
}