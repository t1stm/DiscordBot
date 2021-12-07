using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public class SpotifyTrack : IPlayableItem
    {
        public string Title { get; init; }
        public string Author { get; init; }
        public string TrackId { get; init; }
        public ulong Length { get; init; }
        public string Album { get; init; }

        public DiscordMember Requester { get; set; }

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
            return Length;
        }

        public string GetLocation()
        {
            return null;
        }

        public Task Download()
        {
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

        public string GetId() => "";

        public string GetTypeOf()
        {
            return "Spotify Track";
        }
    }
}