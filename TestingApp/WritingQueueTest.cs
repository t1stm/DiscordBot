using System.IO;
using CustomPlaylistFormat.Objects;
using DiscordBot.Playlists;

namespace TestingApp
{
    public class WritingQueueTest
    {
        public void Test()
        {
            var ms = new MemoryStream();
            var copy = PlaylistThumbnail.GetImage("1aa61601-087a-48d4-bf5d-de3a4a72f64d", new PlaylistInfo(), true, ms);
            copy?.Finish();
            var file = File.Open("./matchFile.bmp", FileMode.Create);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(file);
            file.Close();
        }
    }
}