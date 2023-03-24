#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Tools.Objects;
using vtortola.WebSockets;

namespace DiscordBot.Standalone;

public class FeedableWebSocket : Stream
{
    private const string Message = "This class doesn't support this method.";
    private readonly WebSocketMessageWriteStream BackingStream;
    private readonly Queue<IWriteAction> Cache = new();
    public bool Updating;

    public FeedableWebSocket(WebSocketMessageWriteStream stream)
    {
        BackingStream = stream;
    }

    private bool Closed { get; set; }
    public bool AsynchroniousCopying { get; init; } = true;
    public bool WaitCopy { get; init; } = false;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position { get; set; } = 0;

    private int CacheCount()
    {
        lock (Cache)
        {
            return Cache.Count;
        }
    }

    public void FillBuffer(IWriteAction data)
    {
        lock (Cache)
        {
            Cache.Enqueue(data);
        }

        var updateTask = new Task(UpdateTask);
        updateTask.Start();
        if (WaitCopy) updateTask.Wait();
    }

    private async void UpdateTask()
    {
        try
        {
            if (Updating || Closed) return;
            Updating = true;
            while (CacheCount() != 0 && !Closed)
            {
                IWriteAction? data;
                lock (Cache)
                {
                    data = Cache.Dequeue();
                }

                if (AsynchroniousCopying)
                    await data.WriteToStreamAsync(BackingStream);
                else
                    CopySync(data);
                // Ironic I know. Some streams don't support synchronized writing. Too bad!
            }

            Updating = false;
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Feedable stream update task failed: \"{e}\"");
            Updating = false;
        }
    }

    private void CopySync(IWriteAction data)
    {
        data.WriteToStream(BackingStream);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        FillBuffer(new ByteArrayData(buffer, offset, count));
    }

    public override void Close()
    {
        CloseAsync().Wait();
    }

    public async Task CloseAsync()
    {
        await BackingStream.CloseAsync();
    }

    public override void Flush()
    {
        throw new NotSupportedException(Message);
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await BackingStream.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException(Message);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException(Message);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException(Message);
    }
}