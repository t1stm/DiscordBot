using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Tools.Objects;
using Microsoft.CodeAnalysis;

namespace DiscordBot.Tools
{
    public class StreamSpreader : Stream
    {
        private FeedableStream[] Destinations { get; }
        private CancellationToken Token { get; }

        private long _length;
        private long _position;
        private readonly object LockObject = new();

        public StreamSpreader(CancellationToken token, params Stream[] destinations)
        {
            Destinations = destinations.Select(r => new FeedableStream(r)).ToArray();
            Token = token;
        }

        public async Task Finish()
        {
            await FlushAsync(Token);
            Close();
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

            foreach (var stream in Destinations)
            {
                stream.FlushAsync(Token);
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
            lock (LockObject)
            {
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return;
                    feedableStream.Write(buffer, offset, count);
                }
                _position = _length += count;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (LockObject)
            {
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return Task.CompletedTask;
                    feedableStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
                }
                _position = _length += count;
            }
            return Task.CompletedTask;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (LockObject)
            {
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return;
                    feedableStream.Write(buffer);
                }
                _position = _length += buffer.Length;
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            lock (LockObject)
            {
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return ValueTask.CompletedTask;
                    feedableStream.WriteAsync(buffer, cancellationToken);
                }
                _position = _length += buffer.Length;
            }
            return ValueTask.CompletedTask;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => 0;
            set => _position = value;
        }
    }
}