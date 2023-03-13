using System;
using DiscordBot.Objects;

namespace DiscordBot.Abstract.Errors
{
    public enum NullType
    {
        SearchTerm,
        Attachment,
        Override
    }

    public class NullError : Error
    {
        private readonly NullType _nullType;

        public NullError(NullType nullType)
        {
            _nullType = nullType;
        }
        
        public override string Stringify(ILanguage language)
        {
            return language switch
            {
                Bulgarian => $"Нещо не е било въведено. Код за грешка: \'{_nullType}\'", 
                _ => $"Something wasn't specified. Error code: \'{_nullType}\'"
            };
        }
    }
}