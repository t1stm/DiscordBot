#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CustomPlaylistFormat.Objects;
using DiscordBot.Tools;

namespace DiscordBot.Playlists
{
    public static class PlaylistThumbnail
    {
        public const string WorkingDirectory = $"{PlaylistManager.PlaylistDirectory}/Thumbnails";
        public const string NotFoundImageFilename = "not-found";

        public static Task<StreamSpreader?> GetNotFoundImage(Stream destination)
        {
            return GetImage(NotFoundImageFilename, NotFoundInfo, false, destination);
        }

        public static async Task<StreamSpreader?> GetImage(string? id, PlaylistInfo info, bool overwrite, Stream destination)
        {
            var filename = $"{WorkingDirectory}/{id ?? info.Guid.ToString()}.png";
            //TODO: Generate code that checks for playlist image.
            if (File.Exists(filename) && !overwrite)
            {
                await using var file = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                await file.CopyToAsync(destination);
                return null;
            }
            var newFile = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var streamSpreader = new StreamSpreader(CancellationToken.None, newFile, destination);
            var thumbnailGenerator = new PlaylistThumbnailGenerator.Generator(streamSpreader);
            await thumbnailGenerator.Generate(info);
            return streamSpreader;
        }
        
        private static PlaylistInfo NotFoundInfo => new()
        {
            Name = "Not found.",
            Maker = "You?",
            Description = "The requested playlist wasn't found in the database. Please check the request or I'll kindly come knock on your house with an axe.",
            Count = 0,
            IsPublic = true
        };
    }
}