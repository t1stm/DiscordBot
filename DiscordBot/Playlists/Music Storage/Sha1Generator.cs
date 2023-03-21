#nullable enable
using System;
using System.Security.Cryptography;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class Sha1Generator
    {
        public static string Get(byte[] sourceData)
        {
            var hash = SHA1.HashData(sourceData);
            var hashString = BitConverter.ToString(hash);
            return hashString.Replace("-", string.Empty).ToLower();
        }
    }
}