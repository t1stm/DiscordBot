using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;

namespace BatToshoRESTApp.Audio.Objects
{
    public class OnlineFile : PlayableItem
    {
        public string Url { private get; init; }

        public new string GetName()
        {
            var loc = GetLocation();
            return loc.Length > 40 ? $"{loc[..40]}..." : loc;
        }

        public new string GetLocation()
        {
            return Url;
        }

        public override Task Download()
        {
            return Task.CompletedTask;
        }

        public override string GetId()
        {
            return "";
        }

        public override string GetTypeOf()
        {
            return "Online File";
        }

        public new string GetTitle()
        {
            return GetLocation().Length <= 40 ? GetLocation() : GetLocation()[..40] + "...";
        }

        public new string GetAuthor()
        {
            return "";
        }

        public override string GetThumbnailUrl()
        {
            return null;
        }
    }
}