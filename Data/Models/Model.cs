#nullable enable
using System;
using System.Collections.Generic;

namespace DiscordBot.Data.Models
{
    public abstract class Model<T>
    {
        public abstract T SearchFrom(IEnumerable<T> source);

        public Action? SetModified;
    }
}