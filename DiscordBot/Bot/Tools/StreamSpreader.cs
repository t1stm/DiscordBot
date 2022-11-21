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
        private readonly SemaphoreSlim semaphore = new(1);
        private long _length;
        private long _position;
        public bool KeepCached { get; init; }

        public StreamSpreader(CancellationToken token, params Stream[] destinations)
        {
            Destinations = destinations.Select(r => new FeedableStream(r)).ToList();
            Token = token;
        }

        public async Task ReadStream(Stream stream, bool close = true)
        {
            var buffer = new byte[1 << 12];
            while (await stream.ReadAsync(buffer, Token) != 0)
            {
                await WriteAsync(buffer, 0 ,buffer.Length, Token);
            }

            if (close)
            {
                await stream.DisposeAsync();
                stream.Close();
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
                    await Task.Delay(33, Token);
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
                await stream.FlushAsync(Token).ConfigureAwait(false);
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
            semaphore.Wait(Token);
            var bufferCopy = buffer.ToArray();
            if (KeepCached) DataWritten.Enqueue(new ByteArrayData(bufferCopy, offset, count));
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested) return;
                feedableStream.Write(bufferCopy, offset, count);
            }
            _position = _length += count;
            semaphore.Release();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            var bufferCopy = buffer.ToArray();
            if (KeepCached) DataWritten.Enqueue(new ByteArrayData(bufferCopy, offset, count));
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested)
                {
                    semaphore.Release();
                    return;
                }
                await feedableStream.WriteAsync(bufferCopy, offset, count, cancellationToken);
            }
            _position = _length += count;
            semaphore.Release();
        }

        /*public override void Write(ReadOnlySpan<byte> buffer)
        {
            semaphore.Wait(Token);
            if (KeepCached) DataWritten.Enqueue(new MemoryData(buffer.ToArray()));
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested) return;
                feedableStream.Write(buffer);
            }
            _position = _length += buffer.Length;
            semaphore.Release();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            await semaphore.WaitAsync(cancellationToken);
            if (KeepCached) DataWritten.Enqueue(new MemoryData(buffer));
            foreach (var feedableStream in Destinations)
            {
                if (Token.IsCancellationRequested)
                {
                    semaphore.Release();
                    return;
                }
                await feedableStream.WriteAsync(buffer, cancellationToken);
            }
            _position = _length += buffer.Length;
            semaphore.Release();
        }*/

        public FeedableStream AddDestination(Stream destination, CancellationToken? token = null)
        {
            semaphore.Wait(token ?? CancellationToken.None);
            var feedableStream = new FeedableStream(destination);
            if (KeepCached) foreach (var action in DataWritten.ToArray())
            {
                feedableStream.Write(action);
            }
            Destinations.Add(feedableStream);
            semaphore.Release();
            return feedableStream;
        }
        
        public async Task<FeedableStream> AddDestinationAsync(Stream destination, CancellationToken? token = null)
        {
            await semaphore.WaitAsync(token ?? CancellationToken.None);
            var feedableStream = new FeedableStream(destination);
            if (KeepCached) foreach (var action in DataWritten.ToArray())
            {
                feedableStream.Write(action);
            }

            await feedableStream.FlushAsync(token ?? CancellationToken.None);
            Destinations.Add(feedableStream);
            semaphore.Release();
            return feedableStream;
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