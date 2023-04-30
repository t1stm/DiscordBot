using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Readers;
using Streams;
using Result;
using Result.Objects;

namespace DiscordBot.Audio.Objects;

public class Vbox7Video : PlayableItem
{
    public string Id { get; init; }

    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        var stream_spreader =
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(Location), true);
        if (stream_spreader == Status.Error) return stream_spreader;
            
        stream_spreader.GetOK().AddDestinations(outputs);
        return stream_spreader;
    }

    public override string GetThumbnailUrl()
    {
        // Reference:
        //       https://i49.vbox7.com/o/a5c/a5c864bf710.jpg
        return $"https://i49.vbox7.com/o/{Id[..3]}/{Id}0.jpg"; // Interesting format indeed.
    }

    public override string GetAddUrl()
    {
        return $"vb7://{Id}";
    }

    public override bool GetIfErrored()
    {
        return false;
    }

    public override string GetId()
    {
        return Id;
    }
}