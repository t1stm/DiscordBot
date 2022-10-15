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

        public static Task<StreamSpreader?> GetNotFoundInfo(Stream destination)
        {
            return GetImage(NotFoundImageFilename, NotFoundInfo, false, destination);
        }

        public static async Task<StreamSpreader?> GetImage(string? id, PlaylistInfo info, bool overwrite, Stream destination)
        {
            var filename = $"{WorkingDirectory}/{id ?? info.Guid.ToString()}.png";
            if (File.Exists(filename) && !overwrite)
            {
                await using var file = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                await file.CopyToAsync(destination);
                return null;
            }
            var newFile = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var streamSpreader = new StreamSpreader(CancellationToken.None, newFile, destination);
            var thumbnailGenerator = new PlaylistThumbnailGenerator.Generator(streamSpreader, GetPlaylistImage(info));
            await thumbnailGenerator.Generate(info);
            return streamSpreader;
        }

        private static Stream GetPlaylistImage(PlaylistInfo info)
        {
            string path = $"{WorkingDirectory}/Thumbnail Images/{info.Guid}.png";
            var exists = info.HasThumbnail && File.Exists(path);
            // File.ReadAllBytesAsync("./NoGuildImage.png");
            return exists
                ? File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                : NotFoundPlaylistImage;
        }
        
        public static async Task<StreamSpreader> PlaylistImageSpreader(PlaylistInfo info, Stream destination)
        {
            await using var img = GetPlaylistImage(info);
            var streamSpreader = new StreamSpreader(CancellationToken.None, destination);
            await img.CopyToAsync(streamSpreader);
            return streamSpreader;
        }
        
        public static Stream NotFoundPlaylistImage => File.Open("./NoGuildImage.png", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        public static async Task<StreamSpreader> WriteNotFoundPlaylistImage(Stream destination)
        {
            await using var img = NotFoundPlaylistImage;
            var streamSpreader = new StreamSpreader(CancellationToken.None, destination);
            await img.CopyToAsync(streamSpreader);
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