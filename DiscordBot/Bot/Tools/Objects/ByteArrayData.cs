using System.IO;
using System.Threading.Tasks;

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
            if (destination.CanWrite) 
                destination.Write(Data, Offset, Count);
        }
        public async Task WriteToStreamAsync(Stream destination)
        {
            if (destination.CanWrite)
                await destination.WriteAsync(Data, Offset, Count);
        }
    }
}