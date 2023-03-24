#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;

namespace DiscordBot.Tools.Objects;

public class FeedableStream : Stream
{
    private readonly Stream BackingStream;
    private readonly Queue<IWriteAction> Cache = new();
    public readonly List<Action> FinishedUpdating = new();
    private readonly SemaphoreSlim semaphore = new(1);
    public bool Updating;

    public FeedableStream(Stream backingBackingStream)
    {
        BackingStream = backingBackingStream;
    }

    private bool Closed { get; set; }
    public bool AsynchroniousCopying { get; init; } = true;
    public bool WaitCopy { get; init; } = false;

    /*public override void Write(ReadOnlySpan<byte> buffer)
    {
        semaphore.Wait();
        FillBuffer(new ByteArrayData(buffer.ToArray(), 0, buffer.Length));
        semaphore.Release();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        await semaphore.WaitAsync(cancellationToken);
        FillBuffer(new ByteArrayData(buffer.ToArray(), 0, buffer.Length));
        semaphore.Release();
    }*/

    public override bool CanRead => BackingStream.CanRead;
    public override bool CanSeek => BackingStream.CanSeek;
    public override bool CanWrite => BackingStream.CanWrite;
    public override long Length => BackingStream.Length;

    public override long Position
    {
        get => 0;
        set { }
    }

    public override void Close()
    {
        Closed = true;
        BackingStream.Close();
        base.Close();
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

    private int CacheCount()
    {
        lock (Cache)
        {
            return Cache.Count;
        }
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

            lock (FinishedUpdating)
            {
                foreach (var action in FinishedUpdating) action.Invoke();

                FinishedUpdating.Clear();
            }

            Updating = false;
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Feedable stream update task failed: \"{e}\"");
            Updating = false;
        }
    }

    public Task AwaitFinish(CancellationToken? token = null)
    {
        TaskCompletionSource source = new();
        var action = new Action(() => { source.SetResult(); });
        token?.Register(() => { source.SetCanceled(); });
        lock (FinishedUpdating)
        {
            FinishedUpdating.Add(action);
        }

        return source.Task;
    }

    private void CopySync(IWriteAction data)
    {
        data.WriteToStream(BackingStream);
    }

    public override void Flush()
    {
        UpdateTask();
        BackingStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        UpdateTask();
        return BackingStream.FlushAsync(cancellationToken);
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

    public void Write(IWriteAction action)
    {
        semaphore.Wait();
        FillBuffer(action);
        semaphore.Release();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        semaphore.Wait();
        FillBuffer(new ByteArrayData(buffer, offset, count));
        semaphore.Release();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        FillBuffer(new ByteArrayData(buffer, offset, count));
        semaphore.Release();
    }
}