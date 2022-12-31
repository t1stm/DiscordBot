using System.IO;
using System.Threading.Tasks;
using CustomPlaylistFormat.Objects;
using DiscordBot.Playlists;

namespace TestingApp
{
    public class WritingQueueTest
    {
        public async void Test()
        {
            var ms = new MemoryStream();
            var copy = await PlaylistThumbnail.GetImage("1aa61601-087a-48d4-bf5d-de3a4a72f64d", new PlaylistInfo(),
                true, ms);
            await (copy?.Finish() ?? Task.CompletedTask);
            var file = File.Open("./matchFile.bmp", FileMode.Create);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(file);
            file.Close();
        }
    }
}