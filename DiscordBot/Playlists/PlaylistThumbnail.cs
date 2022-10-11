#nullable enable
using System.IO;
using System.Linq;
using System.Threading;
using CustomPlaylistFormat.Objects;
using DiscordBot.Tools;

namespace DiscordBot.Playlists
{
    public static class PlaylistThumbnail
    {
        public const string WorkingDirectory = $"{PlaylistManager.PlaylistDirectory}/Thumbnails";
        public const string NotFoundImageFilename = "not-found.png";

        public static StreamSpreader? GetNotFoundImage(Stream destination)
        {
            return GetImage(NotFoundImageFilename, NotFoundInfo, false, destination);
        }

        public static StreamSpreader? GetImage(string? id, PlaylistInfo info, bool overwrite, Stream destination)
        {
            var filename = $"{WorkingDirectory}/{id ?? info.Guid.ToString()}.bmp";
            if (File.Exists(filename) && !overwrite)
            {
                using var file = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                file.CopyTo(destination);
                return null;
            }
            var newFile = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
            var streamSpreader = new StreamSpreader(CancellationToken.None, newFile, destination);
            var thumbnailGenerator = new PlaylistThumbnailGenerator.Generator(streamSpreader);
            thumbnailGenerator.Generate(info);
            return streamSpreader;
        }
        
        private static PlaylistInfo NotFoundInfo => new()
        {
            Name = "Not found.",
            Maker = "You?",
            Description = "The requested playlist wasn't found in the database. Please check the request or I'll kindly come to your house.",
            Count = 0,
            IsPublic = true
        };
    }
}