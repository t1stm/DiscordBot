#nullable enable
using DiscordBot.Playlists.Music_Storage.Objects;
using TagLib;

namespace DiscordBot.Playlists.Music_Storage;

public class Id3v2
{
    public static EmbeddedImage GetImageFromTag(string location)
    {
        var file = File.Create(location);
        var tag = file.GetTag(TagTypes.Id3v2);
        if (tag == null)
            return new EmbeddedImage
            {
                HasData = false
            };
        var pictures = tag.Pictures;

        if (pictures.Length < 1)
            return new EmbeddedImage
            {
                HasData = false
            };

        var picture = pictures[0];
        var data = picture.Data.Data;
        var mime = picture.MimeType;

        return new EmbeddedImage
        {
            HasData = true,
            Data = data,
            MimeType = mime
        };
    }
}