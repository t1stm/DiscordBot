#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Tools
{
    public class FeedableStream : Stream
    {
        private Stream BackingStream;
        private List<byte[]> Cache = new();
        private bool Updating;

        public FeedableStream(Stream backingBackingStream)
        {
            BackingStream = backingBackingStream;
        }

        public void FillBuffer(IEnumerable<byte> data)
        {
            lock (Cache) Cache.Add(data.ToArray());
            var updater = new Task(UpdateTask);
            updater.Start();
        }

        private int CacheCount()
        {
            lock (Cache)
            {
                return Cache.Count;
            }
        }
        
        private void UpdateTask()
        {
            if (Updating) return;
            Updating = true;
            while (CacheCount() != 0)
            {
                byte[]? data;
                lock (Cache) data = Cache.FirstOrDefault();
                if (data == null) return;
                BackingStream.Write(data);
                lock (Cache) Cache.Remove(data);
            }

            Updating = false;
        }

        public override void Flush()
        {
            BackingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return BackingStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BackingStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BackingStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BackingStream.Write(buffer, offset, count);
        }

        public override bool CanRead => BackingStream.CanRead;
        public override bool CanSeek => BackingStream.CanSeek;
        public override bool CanWrite => BackingStream.CanWrite;
        public override long Length => BackingStream.Length;
        public override long Position 
        { 
            get => BackingStream.Position; 
            set => BackingStream.Position = value; 
        }
    }
}