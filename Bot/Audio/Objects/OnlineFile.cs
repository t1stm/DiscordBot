using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public class OnlineFile : IPlayableItem
    {
        public string Url { private get; init; }
        private DiscordMember Requester { get; set; }

        public string GetName()
        {
            var loc = GetLocation();
            return loc.Length > 40 ? $"{loc[..40]}..." : loc;
        }

        public ulong GetLength()
        {
            return 0;
        }

        public string GetLocation()
        {
            return Url;
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
            return "Online File";
        }

        public bool GetIfErrored()
        {
            return false;
        }

        public string GetTitle()
        {
            return GetLocation().Length <= 40 ? GetLocation() : GetLocation()[..40] + "...";
        }

        public string GetAuthor()
        {
            return "";
        }

        public string GetThumbnailUrl()
        {
            return null;
        }
    }
}