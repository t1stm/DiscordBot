using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Tools.Objects;

namespace DiscordBot.Tools
{
    public class StreamSpreader : Stream
    {
        private FeedableStream[] Destinations { get; }
        private CancellationToken Token { get; set; }

        public StreamSpreader(CancellationToken token, params Stream[] destinations)
        {
            Destinations = destinations.Select(r => new FeedableStream(r)).ToArray();
            Token = token;
        }

        public override void Close()
        {
            foreach (var feedableStream in Destinations)
            {
                feedableStream.Close();
            }
            base.Close();
        }

        public override void Flush()
        {
            while (Destinations.All(r => r.Updating))
            {
                Task.Delay(33, Token).Wait(Token);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested) return;
                feedableStream.Write(buffer, offset, count);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested) return new ValueTask();
                feedableStream.Write(buffer.ToArray());
            }

            return new ValueTask();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
    }
}