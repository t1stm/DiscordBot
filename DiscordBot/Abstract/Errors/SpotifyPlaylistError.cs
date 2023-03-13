using System;
using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors
{
    public class SpotifyPlaylistError : Error
    {
        public override string Stringify(ILanguage language)
        {
            return language switch
            {
                Bulgarian => "Неуспешно зареждане на Spotify списък.",
                _ => "Unable to load Spotify playlist."
            };
        }
    }
}