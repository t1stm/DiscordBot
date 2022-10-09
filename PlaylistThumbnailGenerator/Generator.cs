using System;
using System.IO;
using CustomPlaylistFormat.Objects;
using ImageMagick;

namespace PlaylistThumbnailGenerator
{
    public class Generator
    {
        private readonly Stream _backingStream;

        public Generator(Stream backingStream)
        {
            _backingStream = backingStream;
        }
        public void Generate(PlaylistInfo playlistInfo)
        {
            MagickNET.SetDefaultFontFile("./AndadaPro.ttf");
            var imageArray = File.ReadAllBytes("./PlaylistImageGradient.png");
            var playlistImage = File.ReadAllBytes("./NoGuildImage.png");

            using var magickImage = new MagickImage(imageArray)
            {
                Format = MagickFormat.Png,
                HasAlpha = true
            };
            using var overlayImage = new MagickImage(playlistImage);
            GeneratePlaylistImage(overlayImage);
            magickImage.Composite(overlayImage, 71, 59, CompositeOperator.Over);
            GenerateText(magickImage, playlistInfo);
            magickImage.Write(_backingStream);
        }

        private static void GenerateText(IMagickImage image, PlaylistInfo info)
        {
            var titleSettings = GetTextSettings(64, 510, 150, Gravity.Southwest);
            var makerSettings = GetTextSettings(32, 510, 78, Gravity.Northwest);
            var descriptionSettings = GetTextSettings(32, 500, 160, Gravity.Southwest);
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
            overlayImage.Scale(420,420);
            using var mask = new MagickImage(MagickColors.Black, overlayImage.Width, overlayImage.Height);
            new Drawables()
                .FillColor(MagickColors.White)
                .StrokeColor(MagickColors.White)
                .RoundRectangle(0,0,overlayImage.Width,overlayImage.Height, overlayImage.Width * 0.5,overlayImage.Height * 0.5)
                .Draw(mask);
            
            mask.HasAlpha = false;
            overlayImage.HasAlpha = false;
            overlayImage.Composite(mask, CompositeOperator.CopyAlpha);
        }
    }
}