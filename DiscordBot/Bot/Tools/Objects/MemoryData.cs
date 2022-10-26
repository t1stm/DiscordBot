using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;

namespace DiscordBot.Tools.Objects
{
    public class MemoryData : IWriteAction
    {
        private readonly ReadOnlyMemory<byte> Data;

        public MemoryData(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public void WriteToStream(Stream destination, CancellationToken? cancellationToken = null)
        {
            try
            {
                if (destination.CanWrite && !(cancellationToken?.IsCancellationRequested ?? false))
                    destination.Write(Data.Span);
            }
            catch (Exception e)
            {
                if (!(cancellationToken?.IsCancellationRequested ?? false))
                    Debug.Write($"ByteArrayData WriteToStreamAsync failed: \"{e}\"");
            }
        }
        public async Task WriteToStreamAsync(Stream destination, CancellationToken? cancellationToken = null)
        {
            try
            {
                if (destination.CanWrite && !(cancellationToken?.IsCancellationRequested ?? false))
                    await destination.WriteAsync(Data, cancellationToken ?? CancellationToken.None);
            }
            catch (Exception e)
            {
                if (!(cancellationToken?.IsCancellationRequested ?? false))
                    await Debug.WriteAsync($"ByteArrayData WriteToStreamAsync failed: \"{e}\"");
            }
        }
    }
}