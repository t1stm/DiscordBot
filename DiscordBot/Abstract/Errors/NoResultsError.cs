using System;
using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors
{
    public class NoResultsError : Error
    {
        public override string Stringify(ILanguage language)
        {
            return language switch
            {
                Bulgarian => "Не бяха намерени резултати.",
                _ => "No results were found.",
            };
        }
    }
}