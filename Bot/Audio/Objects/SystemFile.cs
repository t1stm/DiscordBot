using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using TagLib;

namespace BatToshoRESTApp.Audio.Objects
{
    public class SystemFile : IPlayableItem
    {
        public bool IsDiscordAttachment { get; init; } = true;
        public string Location { get; init; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        private bool Checked { get; set; }

        private DiscordMember Requester { get; set; }

        public string GetTitle()
        {
            return Title;
        }

        public string GetAuthor()
        {
            return Author;
        }

        public string GetThumbnailUrl()
        {
            return "";
        }

        public bool GetIfErrored()
        {
            return false;
        }

        public string GetName()
        {
            return $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";
        }

        public ulong GetLength()
        {
            return Length == default ? 0 : Length;
        }

        public string GetLocation()
        {
            return Location;
        }

        public Task Download()
        {
            if (Checked) return Task.CompletedTask;
            Checked = true;
            try
            {
                var info = File.Create(Location);
                Length = (ulong) info.Properties.Duration.TotalMilliseconds + 0; //Fixed: 15 Mar 2022 How can I be this dumb.
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

        public void SetRequester(DiscordMember member)
        {
            Requester = member;
        }

        public DiscordMember GetRequester()
        {
            return Requester;
        }

        public string GetId()
        {
            return "";
        }

        public string GetTypeOf()
        {
            return IsDiscordAttachment ? "Discord Attachment" : "Local File";
        }
    }
}