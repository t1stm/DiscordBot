using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Tools.Objects
{
    public class MemoryData : IWriteAction
    {
        private readonly ReadOnlyMemory<byte> Data;

        public MemoryData(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public void WriteToStream(Stream destination)
        {
            if (destination.CanWrite)
                destination.Write(Data.Span);
        }

        public async Task WriteToStreamAsync(Stream destination)
        {
            if (destination.CanWrite)
                await destination.WriteAsync(Data);
        }
    }
}