using System.Collections.Generic;

namespace DiscordBot.Data.Models
{
    public interface IModel<T>
    {
        public T Read(IEnumerable<T> source);
    }
}