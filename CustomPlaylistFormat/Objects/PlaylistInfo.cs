#nullable enable
namespace CustomPlaylistFormat.Objects;

public class PlaylistInfo
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Maker { get; set; }
    public long LastModified { get; set; }
    public bool IsAnonymous => string.IsNullOrEmpty(Maker);
    public bool IsPublic { get; set; } = true;
    public bool HasThumbnail { get; set; }
    public uint Count { get; set; }

    public Guid? Guid { get; set; }

    public void SetUpdated()
    {
        HasThumbnail = false;
    }

    public override string ToString()
    {
        return $"Name: '{Name}', Description: '{Description}', Maker: '{Maker}'";
    }
}