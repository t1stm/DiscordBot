using System;

namespace DiscordBot.Abstract;

public class InvalidResultAccessException : Exception
{
    public InvalidResultAccessException(string message) : base(message)
    {
    }
}