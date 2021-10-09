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
        private async Task<string> GenerateComposite(string image1, int x1, int y1, DiscordUser user1, int res,
            string image2 = null, int? x2 = null, int? y2 = null, DiscordUser user2 = null,
            string path = "", string baseImage = "cursed_touch2.jpg")
        {
            string fileName;
            if (user1 != null && user2 != null)
            {
                fileName = $"{path}{user1.Username}-{user2.Username}.png";
                fileName = fileName.Replace(" ", "_");
                if (File.Exists($"/srv/http/Bat_Tosho_Content/{fileName}"))
                    return $"http://dank.gq/Bat_Tosho_Content/{fileName}";
            }
            else if (user2 == null && image2 == null && user1 is not null)
            {
                fileName = $"{path}{user1.Username}.png";
                fileName = fileName.Replace(" ", "_");
                if (File.Exists($"/srv/http/Bat_Tosho_Content/{fileName}"))
                    return $"http://dank.gq/Bat_Tosho_Content/{fileName}";
            }
            else
            {
                // ReSharper disable once RedundantAssignment
                fileName = "BuggedFileName.png";
                throw new WebException(
                    "Didn't receive Discord Users on GenerateComposite method. This shouldn't happen, but I made" +
                    " the method contain null so that the bot doesn't completely crash if an error occurs. I hope " +
                    "I don't see this message anywhere else.");
            }

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(image1, "/srv/http/Bat_Tosho_Content/image1.png");
                if (image2 != null)
                    await client.DownloadFileTaskAsync(image2, "/srv/http/Bat_Tosho_Content/image2.png");
            }

            await Debug.Write($"Image 1 is: {image1}");
            await Debug.Write($"Image 2 is: {image2}");
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
                    await Debug.Write($"Link is: {url}");
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
                ImageTypes.Dick => await GenerateComposite(du2.AvatarUrl, 220, 53, du2, 200, du1.AvatarUrl, 420,
                    33, du1),
                ImageTypes.Monke => await GenerateComposite(du1.AvatarUrl, 435, 60, du1, 512, null, null, null,
                    null, "Monke/", "monke.jpg"),
                _ => null
            };
        }
    }
}