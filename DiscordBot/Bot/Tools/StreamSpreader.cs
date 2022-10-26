using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;
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
        public bool KeepCached { get; init; }

        public StreamSpreader(CancellationToken token, params Stream[] destinations)
        {
            lock (LockObject)
            {
                Destinations = destinations.Select(r => new FeedableStream(r)).ToList();
                Token = token;
            }
        }

        public async Task Finish()
        {
            await FlushAsync(Token);
            Close();
        }

        public async Task CloseWhenCopied()
        {
            try
            {
                while (Destinations.Any(r => r.Updating))
                {
                    if (Token.IsCancellationRequested) break;
                    await Task.Delay(250, Token);
                }

                await FlushAsync(Token);
                Close();
            }
            catch (Exception e)
            {
                if (!Token.IsCancellationRequested)
                    await Debug.WriteAsync($"StreamSpreader CloseWhenCopied failed: \"{e}\"");
            }
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
                if (cancellationToken.IsCancellationRequested) return;
                await Task.Delay(33, Token);
            }
            
            foreach (var stream in Destinations)
            {
                await stream.FlushAsync(Token);
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
                if (KeepCached) DataWritten.Enqueue(new ByteArrayData(buffer, offset, count));
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
                if (KeepCached) DataWritten.Enqueue(new ByteArrayData(buffer, offset, count));
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
                if (KeepCached) DataWritten.Enqueue(new MemoryData(buffer.ToArray()));
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
                if (KeepCached) DataWritten.Enqueue(new MemoryData(buffer));
                foreach (var feedableStream in Destinations)
                {
                    if (Token.IsCancellationRequested) return ValueTask.CompletedTask;
                    feedableStream.WriteAsync(buffer, cancellationToken);
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
                if (KeepCached) foreach (var action in DataWritten.ToArray())
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