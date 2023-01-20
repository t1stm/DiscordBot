#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = DiscordBot.Methods.Debug;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class Flac
    {
        public static FlacImage GetImageFromFile(string location)
        {
            if (!File.Exists(location) || !location.Contains(".flac"))
            {
                return new FlacImage
                {
                    HasData = false
                };
            }

            var process = Process.Start(new ProcessStartInfo
            {
                Arguments = $"--export-picture-to=- \"{location}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "metaflac"
            });
            if (process == null)
            {
                return new FlacImage
                {
                    HasData = false
                };
            }
            process.Start();

            var memoryStream = new MemoryStream();
            var data = process.StandardOutput.BaseStream;
            data.CopyTo(memoryStream);
            memoryStream.Position = 0;
            var array = memoryStream.ToArray();

            if (array.Length < 1)
            {
                return new FlacImage
                {
                    HasData = false
                };
            }

            return new FlacImage
            {
                HasData = true,
                Data = array,
                MimeType = ""
            };
        }

        public static string GetImageFiletype(byte[] data)
        {
            byte[] PngHeader = {137,80,78,71,13,10,26,10};
            byte[] JpegHeader = {255, 216, 255};
            return data.Length switch
            {
                > 9 when HasHeader(PngHeader, data) => "png",
                > 5 when HasHeader(JpegHeader, data) => "jpg",
                _ => ""
            };
        }

        private static bool HasHeader(IEnumerable<byte> header, IReadOnlyList<byte> source)
        {
            return !header.Where((t, i) => source[i] != t).Any();
        }
        
        public struct FlacImage
        {
            public bool HasData;
            public byte[]? Data;
            public string? MimeType;
        }
    }
}