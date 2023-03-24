#nullable enable
using System;
using System.Collections.Generic;

namespace DiscordBot.Data.Models;

public abstract class Model<T>
{
    public Action? SetModified;
    public abstract T? SearchFrom(IEnumerable<T> source);

    public virtual void OnLoaded()
    {
        // To be overridden.
    }
}