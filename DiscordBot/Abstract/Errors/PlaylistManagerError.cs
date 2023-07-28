using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors;

public enum PlaylistManagerErrorType
{
    InvalidUrl,
    InvalidRequest,
    NotFound
}

public class PlaylistManagerError : Error
{
    private readonly PlaylistManagerErrorType _errorType;

    public PlaylistManagerError(PlaylistManagerErrorType errorType)
    {
        _errorType = errorType;
    }

    public override string Stringify(AbstractLanguage language)
    {
        return language switch
        {
            Bulgarian => $"Зареждане на списък завърши с грешка: \'{_errorType}\'",
            _ => $"Loading playlist failed with code: \'{_errorType}\'"
        };
    }
}