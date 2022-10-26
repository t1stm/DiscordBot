using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Tools.Objects
{
    public interface IWriteAction
    {
        public void WriteToStream(Stream destination, CancellationToken? cancellationToken = null);
        public Task WriteToStreamAsync(Stream destination, CancellationToken? cancellationToken = null);
    }
}