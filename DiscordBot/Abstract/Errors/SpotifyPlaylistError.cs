using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class SpotifyPlaylistError : Error
{
    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => "Неуспешно зареждане на Spotify списък.",
            _ => "Unable to load Spotify playlist."
        };
    }
}