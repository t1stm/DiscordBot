using System.Threading.Tasks;
using BatToshoRESTApp.Controllers;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Abstract
{
    public abstract class PlayableItem
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        public string Location { get; set; }
        public DiscordMember Requester { get; set; }
        protected bool Errored { get; set; }

        public string GetName() =>
            $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";

        public ulong GetLength() => Length;
        public string GetLocation() => Location;
        public abstract Task Download();

        public void SetRequester(DiscordMember user)
        {
            Requester = user;
        }

        public DiscordMember GetRequester() => Requester;
        public abstract string GetId();
        public abstract string GetTypeOf();

        public bool GetIfErrored() => Errored;

        public string GetTitle() => Title;

        public string GetAuthor() => Author;

        public abstract string GetThumbnailUrl();

        public BatTosho.SearchResult ToSearchResult()
        {
            return new()
            {
                Title = Title,
                Author = Author,
                IsSpotify = false,
                Length = Length + "",
                ThumbnailUrl = GetThumbnailUrl(),
                Url = "",
                Index = 0,
                VoiceUsers = 0,
                Id = GetId()
            };
        }
    }
}