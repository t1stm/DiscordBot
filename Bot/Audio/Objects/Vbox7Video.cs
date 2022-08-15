using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Readers;

namespace DiscordBot.Audio.Objects
{
    public class Vbox7Video : PlayableItem
    {
        public string Id { get; init; }
        public override async Task GetAudioData(params Stream[] outputs)
        {
            await HttpClient.ChunkedDownloaderToStream(HttpClient.WithCookies(), new Uri(Location), false, outputs);
        }

        public override string GetThumbnailUrl()
        {
            return null;
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
}