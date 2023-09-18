using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public class SearchError : Error
{
    public readonly string ErrorMessage;

    public SearchError(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => $"Грешка при търсене чрез чужд код: \'{ErrorMessage}\'",
            _ => $"Error encountered while searching using a non-local library: \'{ErrorMessage}\'"
        };
    }
}