using System.IO;
using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using DiscordBot.Objects;
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
        public bool Processed { get; set; }

        public virtual string GetName(bool settingsShowOriginalInfo = false)
        {
            return $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";
        }

        public virtual ulong GetLength()
        {
            return Length;
        }

        public virtual string GetLocation()
        {
            return Location;
        }

        public abstract Task<bool> GetAudioData(params Stream[] outputs);

        public virtual Task ProcessInfo()
        {
            Processed = true;
            return Task.CompletedTask;
        }

        public void SetRequester(DiscordMember user)
        {
            Requester = user;
        }

        public DiscordMember GetRequester()
        {
            return Requester;
        }

        public abstract string GetId();

        public string GetTypeOf(ILanguage language)
        {
            return language.GetTypeOfTrack(this);
        }

        public virtual bool GetIfErrored()
        {
            return Errored;
        }

        public virtual string GetTitle()
        {
            return Title;
        }

        public virtual string GetAuthor()
        {
            return Author;
        }

        public abstract string GetThumbnailUrl();

        public abstract string GetAddUrl();

        public virtual SearchResult ToSearchResult()
        {
            return new()
            {
                Title = GetTitle(),
                Author = GetAuthor(),
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