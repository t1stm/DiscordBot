using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class CacheNotFoundError : Error
{
    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => "Не бе открит от кеша.",
            _ => "Unable to find from cache."
        };
    }
}