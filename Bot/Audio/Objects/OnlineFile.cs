using System;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Objects;

namespace DiscordBot.Audio.Objects
{
    public class OnlineFile : PlayableItem
    {
        public override string GetName()
        {
            var loc = GetLocation();
            return loc.Length > 40 ? $"{loc[..40]}..." : loc;
        }

        public override Task Download()
        {
            return Task.CompletedTask;
        }

        public override string GetId()
        {
            return "";
        }

        public override string GetTypeOf(ILanguage language)
        {
            return language switch
            {
                English => "Online File",
                Bulgarian => "Онлайн Файл",
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
            };
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