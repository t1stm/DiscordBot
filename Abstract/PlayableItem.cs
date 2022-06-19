using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace DiscordBot.Abstract
{
    public abstract class PlayableItem
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        public string Location { get; set; }
        public DiscordMember Requester { get; set; }
        protected bool Errored { get; set; }

        public string GetName()
        {
            return $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";
        }

        public ulong GetLength()
        {
            return Length;
        }

        public string GetLocation()
        {
            return Location;
        }

        public abstract Task Download();

        public void SetRequester(DiscordMember user)
        {
            Requester = user;
        }

        public DiscordMember GetRequester()
        {
            return Requester;
        }

        public abstract string GetId();
        public abstract string GetTypeOf();

        public bool GetIfErrored()
        {
            return Errored;
        }

        public string GetTitle()
        {
            return Title;
        }

        public string GetAuthor()
        {
            return Author;
        }

        public abstract string GetThumbnailUrl();

        protected abstract string GetAddUrl();

        public Controllers.Bot.SearchResult ToSearchResult()
        {
            return new()
            {
                Title = Title,
                Author = Author,
                IsSpotify = false,
                Length = Length + "",
                ThumbnailUrl = GetThumbnailUrl(),
                Url = GetAddUrl(),
                //Index = 0,
                //VoiceUsers = 0,
                Id = GetId()
            };
        }
    }
}