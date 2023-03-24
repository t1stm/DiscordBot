using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;

namespace DiscordBot.Audio.Objects;

public class SpotifyTrack : PlayableItem
{
    public string TrackId { get; init; }
    public string Album { get; init; }

    public bool Explicit { get; init; }

    public override Task<bool> GetAudioData(params Stream[] outputs)
    {
        return new Task<bool>(() => false);
    }

    public override string GetId()
    {
        return "";
    }

    public override string GetThumbnailUrl()
    {
        return "";
    }

    public override string GetAddUrl()
    {
        return $"spt://{TrackId}";
    }

    public override SearchResult ToSearchResult()
    {
        return new SearchResult
        {
            Title = GetTitle(),
            Author = GetAuthor(),
            IsSpotify = true,
            Length = Length + "",
            ThumbnailUrl = GetThumbnailUrl(),
            Url = GetAddUrl(),
            Id = GetId()
        };
    }
}