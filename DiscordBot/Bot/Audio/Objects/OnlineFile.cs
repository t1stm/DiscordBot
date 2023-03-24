using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Methods;
using DiscordBot.Readers;

namespace DiscordBot.Audio.Objects;

public class OnlineFile : PlayableItem
{
    public override string GetName(bool settingsShowOriginalInfo = false)
    {
        var loc = GetLocation();
        return loc.Length > 40 ? $"{loc[..40]}..." : loc;
    }

    public override async Task<bool> GetAudioData(params Stream[] outputs)
    {
        try
        {
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(Location), false, outputs);
            return true;
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"OnlineFile GetAudioData method failed: \"{e}\"");
            return false;
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