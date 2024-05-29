using System.IO;
using System.Threading.Tasks;
using DiscordBot.Enums;
using DiscordBot.Readers;
using DSharpPlus.Entities;
using ImageMagick;

namespace DiscordBot.Methods;

public static class ImageMagick
{
    private static async Task<Stream> GenerateComposite(string image1, int x1, int y1, int res,
        string image2 = null, int? x2 = null, int? y2 = null, string baseImage = "cursed_touch.webp")
    {
        var ms = new MemoryStream();
        // await client.DownloadFileTaskAsync(image1, "/srv/http/Bat_Tosho_Content/image1.png");
        // if (image2 != null)
        //     await client.DownloadFileTaskAsync(image2, "/srv/http/Bat_Tosho_Content/image2.png");
        var im1 = await HttpClient.DownloadStream(image1);
        var im2 = image2 switch { null => Stream.Null, _ => await HttpClient.DownloadStream(image2) };

        using var image = new MagickImage($"/srv/http/{Bot.MainDomain}/Bat_Tosho_Content/{baseImage}");
        using var watermark1 = new MagickImage(im1);
        watermark1.Resize(res, res);
        image.Composite(watermark1, x1, y1, CompositeOperator.Over);
        if (x2 != null && y2 != null)
        {
            using var watermark2 = new MagickImage(im2);
            watermark2.Resize(res, res);
            image.Composite(watermark2, x2.Value, y2.Value, CompositeOperator.Over);
        }

        await image.WriteAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public static async Task<Stream> DiscordUserHandler(DiscordUser du1, DiscordUser du2, ImageTypes imageTypes)
    {
        await Debug.WriteAsync($"Generating image of type: {imageTypes}");
        return imageTypes switch
        {
            ImageTypes.Dick => await GenerateComposite(du2.AvatarUrl, 133, 0, 120, du1.AvatarUrl, 420,
                25),
            ImageTypes.Monke => await GenerateComposite(du1.AvatarUrl, 435, 60, 512, null, null, null, "monke.jpg"),
            _ => null
        };
    }
}