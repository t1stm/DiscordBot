using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public abstract class Error
{
    public abstract string Stringify(ILanguage language);
}