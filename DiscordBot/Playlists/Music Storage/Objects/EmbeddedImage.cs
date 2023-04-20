#nullable enable

namespace DiscordBot.Playlists.Music_Storage.Objects;

public struct EmbeddedImage
{
    public bool HasData;
    public byte[]? Data;
    public string? MimeType;
}