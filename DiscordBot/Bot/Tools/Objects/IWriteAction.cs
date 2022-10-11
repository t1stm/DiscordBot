using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Tools.Objects
{
    public interface IWriteAction
    {
        public void WriteToStream(Stream destination);
        public Task WriteToStreamAsync(Stream destination);
    }
}