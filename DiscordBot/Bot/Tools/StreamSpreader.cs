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
        private CancellationToken Token { get; }

        private long _length;
        private long _position;

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
            while (Destinations.Any(r => r.Updating))
            {
                Task.Delay(33, Token).Wait(Token);
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            while (Destinations.Any(r => r.Updating))
            {
                await Task.Delay(33, Token);
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
            _position = _length += count;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested) return;
                await feedableStream.WriteAsync(buffer, cancellationToken);
            }
            _position = _length += buffer.Length;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }
    }
}