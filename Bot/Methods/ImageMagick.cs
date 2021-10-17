using System.IO;
using System.Net;
using System.Threading.Tasks;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;
using ImageMagick;

namespace Bat_Tosho.Methods
{
    public class ImageMagick
    {
        private async Task<string> GenerateComposite(string image1, int x1, int y1, int res,
            string image2 = null, int? x2 = null, int? y2 = null,
            string path = "", string baseImage = "cursed_touch.jpg")
        {
            string fileName = $"{path}{Return.RandomString(16)}.png";

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(image1, "/srv/http/Bat_Tosho_Content/image1.png");
                if (image2 != null)
                    await client.DownloadFileTaskAsync(image2, "/srv/http/Bat_Tosho_Content/image2.png");
            }
            string url;

            using (var image = new MagickImage($"/srv/http/Bat_Tosho_Content/{baseImage}"))
            {
                using (var watermark1 = new MagickImage("/srv/http/Bat_Tosho_Content/image1.png"))
                {
                    watermark1.Resize(res, res);
                    image.Composite(watermark1, x1, y1, CompositeOperator.Over);
                    if (x2 != null && y2 != null)
                    {
                        using var watermark2 = new MagickImage("/srv/http/Bat_Tosho_Content/image2.png");
                        watermark2.Resize(res, res);
                        image.Composite(watermark2, x2.Value, y2.Value, CompositeOperator.Over);
                    }

                    url = $"http://dank.gq/Bat_Tosho_Content/{fileName}";
                    File.Delete($"/srv/http/Bat_Tosho_Content/{fileName}");
                    await image.WriteAsync($"/srv/http/Bat_Tosho_Content/{fileName}");
                }
            }

            return url;
        }

        public async Task<string> DiscordUserHandler(DiscordUser du1, DiscordUser du2, ImageTypes imageTypes)
        {
            await Debug.Write($"Generating image of type: {imageTypes}");
            return imageTypes switch
            {
                ImageTypes.Dick => await GenerateComposite(du1.AvatarUrl, 128, 0, 128, du2.AvatarUrl, 420, 26),
                ImageTypes.Monke => await GenerateComposite(du1.AvatarUrl, 435, 60, 512, null, null, null, "Monke/", "monke.jpg"),
                _ => null
            };
        }
    }
}