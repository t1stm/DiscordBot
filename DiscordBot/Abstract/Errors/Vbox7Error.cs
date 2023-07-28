using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class Vbox7Error : Error
{
    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => "Неуспешно зареждане на Vbox7 видео.",
            _ => "Unable to load Vbox7 video."
        };
    }
}