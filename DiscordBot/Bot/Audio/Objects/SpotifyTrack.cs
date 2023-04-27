using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using Result;
using Streams;

namespace DiscordBot.Audio.Objects;

public class SpotifyTrack : PlayableItem
{
    public string TrackId { get; init; }
    public string Album { get; init; }

    public bool Explicit { get; init; }

    public override Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        return Task.FromResult(Result<StreamSpreader, Error>.Error(new NoResultsError()));
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