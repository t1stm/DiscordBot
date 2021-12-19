using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public class SystemFile : IPlayableItem
    {
        public bool IsDiscordAttachment { get; init; } = true;
        public string Location { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }

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
            return null;
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

        public string GetId() => "";

        public string GetTypeOf()
        {
            return IsDiscordAttachment ? "Discord Attachment" : "Local File";
        }
    }
}