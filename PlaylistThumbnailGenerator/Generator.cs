using System;
using System.IO;
using System.Threading.Tasks;
using CustomPlaylistFormat.Objects;
using ImageMagick;

namespace PlaylistThumbnailGenerator
{
    public class Generator
    {
        private readonly Stream _backingStream;
        private readonly Stream _playlistImageStream;

        public Generator(Stream backingStream, Stream playlistImageStream)
        {
            _backingStream = backingStream;
            _playlistImageStream = playlistImageStream;
        }
        public async Task Generate(PlaylistInfo playlistInfo)
        {
            MagickNET.SetDefaultFontFile("./RobotoSlab.ttf");
            var imageArray = await File.ReadAllBytesAsync("./PlaylistImageGradient.png");

            using var magickImage = new MagickImage(imageArray)
            {
                Format = MagickFormat.Png,
                HasAlpha = true
            };
            using var overlayImage = new MagickImage(_playlistImageStream);
            GeneratePlaylistImage(overlayImage);
            magickImage.Composite(overlayImage, 71, 59, CompositeOperator.Over);
            GenerateText(magickImage, playlistInfo);
            await magickImage.WriteAsync(_backingStream);
        }

        private static void GenerateText(IMagickImage image, PlaylistInfo info)
        {
            var titleSettings = GetTextSettings(64, 510, 150, Gravity.Southwest);
            var makerSettings = GetTextSettings(32, 510, 78, Gravity.Northwest);
            var descriptionSettings = GetTextSettings(32, 500, 200, Gravity.Northwest);
            var countSettings = GetTextSettings(24, 150, 30, Gravity.East);

            GenerateImageEntry(info.Name, 620, 19, titleSettings, image);
            GenerateImageEntry($"Made by: {info.Maker}", 620, 182, makerSettings, image);
            GenerateImageEntry(info.Description, 620, 260, descriptionSettings, image);
            GenerateImageEntry($"{info.Count} {(info.Count == 1 ? "item": "items")}", 950, 460, countSettings, image);
            
        }

        private static void GenerateImageEntry(string? textToWrite, int x, int y, MagickReadSettings settings, IMagickImage image)
        {
            using var caption = new MagickImage($"caption:{textToWrite}", settings);
            image.Composite(caption, x, y, CompositeOperator.Over);
        }

        private static MagickReadSettings GetTextSettings(int fontSize, int width, int height, Gravity textGravity) =>
            new()
            {
                FontWeight = FontWeight.Medium,
                FillColor = MagickColors.White,
                FontPointsize = fontSize,
                TextGravity = textGravity,
                BackgroundColor = MagickColors.Transparent,
                Height = height,
                Width = width
            };

        private static void GeneratePlaylistImage(MagickImage? overlayImage)
        {
            if (overlayImage == null) throw new NullReferenceException($"Variable \'{nameof(overlayImage)}\' in AddPlaylistImage method is null.");
            overlayImage.Scale(-1, 420);
            overlayImage.Crop(420,420, Gravity.Center);
            using var mask = new MagickImage(MagickColors.Black, overlayImage.Height, overlayImage.Height);
            new Drawables()
                .FillColor(MagickColors.White)
                .StrokeColor(MagickColors.White)
                .RoundRectangle(0,0,
                    overlayImage.Height,overlayImage.Height, 
                    overlayImage.Height * 0.5,overlayImage.Height * 0.5)
                .Draw(mask);
            
            mask.HasAlpha = false;
            overlayImage.HasAlpha = false;
            overlayImage.Composite(mask, CompositeOperator.CopyAlpha);
        }
    }
}