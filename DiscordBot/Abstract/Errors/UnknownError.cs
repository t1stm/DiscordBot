using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class UnknownError : Error
{
    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => "Непозната грешка.",
            _ => "Unknown error."
        };
    }
}