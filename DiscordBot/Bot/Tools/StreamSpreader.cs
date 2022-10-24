using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Tools.Objects;

namespace DiscordBot.Tools
{
    public class StreamSpreader : Stream
    {
        private List<FeedableStream> Destinations { get; }
        private Queue<IWriteAction> DataWritten { get; } = new();
        private CancellationToken Token { get; }

        private long _length;
        private long _position;
        private readonly object LockObject = new();

        public StreamSpreader(CancellationToken token, params Stream[] destinations)
        {
            Destinations = destinations.Select(r => new FeedableStream(r)).ToList();
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
                DataWritten.Enqueue(new ByteArrayData(buffer, offset, count));
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
                DataWritten.Enqueue(new ByteArrayData(buffer, offset, count));
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return Task.CompletedTask;
                    feedableStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask().Wait(cancellationToken);
                }
                _position = _length += count;
            }
            return Task.CompletedTask;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (LockObject)
            {
                DataWritten.Enqueue(new MemoryData(buffer.ToArray()));
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
                DataWritten.Enqueue(new MemoryData(buffer));
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return ValueTask.CompletedTask;
                    feedableStream.WriteAsync(buffer, cancellationToken).AsTask().Wait(cancellationToken);
                }
                _position = _length += buffer.Length;
            }
            return ValueTask.CompletedTask;
        }

        public void AddDestination(Stream destination)
        {
            lock (LockObject)
            {
                var feedableStream = new FeedableStream(destination);
                foreach (var action in DataWritten.ToArray())
                {
                    feedableStream.Write(action);
                }
                Destinations.Add(feedableStream);
            }
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