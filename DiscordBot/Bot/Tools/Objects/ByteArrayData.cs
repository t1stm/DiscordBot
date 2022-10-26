using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;

namespace DiscordBot.Tools.Objects
{
    public class ByteArrayData : IWriteAction
    {
        private readonly byte[] Data;
        private readonly int Offset;
        private readonly int Count;

        public ByteArrayData(byte[] data, int offset, int count)
        {
            Data = data;
            Offset = offset;
            Count = count;
        }

        public void WriteToStream(Stream destination, CancellationToken? cancellationToken = null)
        {
            try
            {
                if (destination.CanWrite && !(cancellationToken?.IsCancellationRequested ?? false))
                    destination.Write(Data, Offset, Count);
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
                    await destination.WriteAsync(Data, Offset, Count, cancellationToken ?? CancellationToken.None);
            }
            catch (Exception e)
            {
                if (!(cancellationToken?.IsCancellationRequested ?? false))
                    await Debug.WriteAsync($"ByteArrayData WriteToStreamAsync failed: \"{e}\"");
            }
        }
    }
}