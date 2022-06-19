using System.Threading.Tasks;
using DiscordBot.Abstract;

namespace DiscordBot.Audio.Objects
{
    public class Vbox7Video : PlayableItem
    {
        public string Id { get; init; }
        public override Task Download()
        {
            return Task.CompletedTask;
        }

        public override string GetThumbnailUrl()
        {
            return null;
        }

        protected override string GetAddUrl()
        {
            return $"https://www.vbox7.com/play:{Id}";
        }

        public new bool GetIfErrored()
        {
            return false;
        }

        public override string GetId()
        {
            return Id;
        }

        public override string GetTypeOf()
        {
            return "Vbox7 Video";
        }
    }
}