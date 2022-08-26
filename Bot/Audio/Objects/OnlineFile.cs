using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Readers;

namespace DiscordBot.Audio.Objects
{
    public class OnlineFile : PlayableItem
    {
        public override string GetName(bool settingsShowOriginalInfo = false)
        {
            var loc = GetLocation();
            return loc.Length > 40 ? $"{loc[..40]}..." : loc;
        }

        public override async Task GetAudioData(params Stream[] outputs)
        {
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(Location), false, outputs);
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
}