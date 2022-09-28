using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Methods;
using DiscordBot.Readers;

namespace DiscordBot.Audio.Objects
{
    public class Vbox7Video : PlayableItem
    {
        public string Id { get; init; }

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