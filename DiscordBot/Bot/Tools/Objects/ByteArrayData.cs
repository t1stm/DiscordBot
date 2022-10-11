using System.IO;
using System.Threading.Tasks;
using System;

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

        public void WriteToStream(Stream destination)
        {
            destination.Write(Data, Offset, Count);
        }
        public async Task WriteToStreamAsync(Stream destination)
        {
            await destination.WriteAsync(Data.AsMemory(Offset, Count));
        }
    }
}