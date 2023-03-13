using System;
using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors
{
    public enum PlaylistManagerErrorType
    {
        InvalidUrl,
        InvalidRequest,
        NotFound
    }
    
    public class PlaylistManagerError : Error
    {
        public PlaylistManagerError(PlaylistManagerErrorType errorType)
        {
            _errorType = errorType;
        }

        private readonly PlaylistManagerErrorType _errorType;
        public override string Stringify(ILanguage language)
        {
            return language switch
            {
                Bulgarian => $"Зареждане на списък завърши с грешка: \'{_errorType}\'",
                _ => $"Loading playlist failed with code: \'{_errorType}\'"
            };
        }
    }
}