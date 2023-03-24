#nullable enable
using DiscordBot.Methods;
using TagLib;

namespace DiscordBot.Playlists.Music_Storage;

public class Id3v2
{
    public static Id3Image GetImageFromTag(string location)
    {
        var file = File.Create(location);
        var tag = file.GetTag(TagTypes.Id3v2);
        if (tag == null)
            return new Id3Image
            {
                HasData = false
            };
        var pictures = tag.Pictures;

        if (pictures.Length < 1)
        {
            Debug.Write("No image found in Id3v2 tag.");
            return new Id3Image
            {
                HasData = false
            };
        }

        var picture = pictures[0];
        var data = picture.Data.Data;
        var mime = picture.MimeType;

        return new Id3Image
        {
            HasData = true,
            Data = data,
            MimeType = mime
        };
    }
}

public struct Id3Image
{
    public bool HasData;
    public byte[]? Data;
    public string? MimeType;
}