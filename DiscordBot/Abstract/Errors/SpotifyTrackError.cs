using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class SpotifyTrackError : Error
{
    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => "Неуспешно зареждане на Spotify песен.",
            _ => "Unable to load Spotify song."
        };
    }
}