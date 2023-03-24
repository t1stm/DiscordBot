using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class UnknownError : Error
{
    public override string Stringify(ILanguage language)
    {
        return language switch
        {
            Bulgarian => "Непозната грешка.",
            _ => "Unknown error."
        };
    }
}