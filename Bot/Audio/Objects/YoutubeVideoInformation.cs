using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public class YoutubeVideoInformation : IPlayableItem
    {
        public string SearchTerm { get; init; }
        public bool IsId { get; init; } = false;
        public string YoutubeId { get; set; }
        public SpotifyTrack OriginTrack { get; set; } = null;
        public string Location { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ThumbnailUrl { get; set; }
        public ulong Length { get; set; }

        public DiscordMember Requester { get; set; }

        public string GetLocation()
        {
            return Location;
        }

        public async Task Download()
        {
            if (string.IsNullOrEmpty(Location)) Location = await Downloader.Download(YoutubeId);
        }

        public string GetName()
        {
            return $"{Title}{string.IsNullOrEmpty(Author) switch {false => $" - {Author}", true => ""}}";
        }

        public ulong GetLength()
        {
            return Length;
        }
        public void SetRequester(DiscordMember member) => Requester = member;
        public DiscordMember GetRequester() => Requester;
    }
}