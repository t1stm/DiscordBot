using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using DiscordBot.Readers;
using Streams;
using Result;
using Result.Objects;

namespace DiscordBot.Audio.Objects;

public class OnlineFile : PlayableItem
{
    public override string GetName(bool settingsShowOriginalInfo = false)
    {
        var loc = GetLocation();
        return loc.Length > 40 ? $"{loc[..40]}..." : loc;
    }

    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        try
        {
            var stream_spreader =
                await HttpClient.ChunkedDownloader(HttpClient.WithCookies(), new Uri(Location));
            if (stream_spreader == Status.Error) return stream_spreader;
            
            stream_spreader.GetOK().AddDestinations(outputs);
            return stream_spreader;
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"OnlineFile GetAudioData method failed: \"{e}\"");
            return Result<StreamSpreader, Error>.Error(new UnknownError());
        }
    }

    public override string GetId()
    {
        return "";
    }

    public override string GetTitle()
    {
        return GetLocation().Length <= 40 ? GetLocation() : GetLocation()[..40] + "...";
    }

    public override string GetAuthor()
    {
        return "";
    }

    public override string GetThumbnailUrl()
    {
        return null;
    }

    public override string GetAddUrl()
    {
        return GetLocation();
    }
}