#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Methods;

namespace DiscordBot.Tools.Objects
{
    public class FeedableStream : Stream
    {
        private readonly Stream BackingStream;
        private readonly Queue<StreamData> Cache = new();
        public bool Updating;
        private bool Closed { get; set; }
        public bool WaitCopy { get; init; } = false;

        public FeedableStream(Stream backingBackingStream)
        {
            BackingStream = backingBackingStream;
        }

        public override void Close()
        {
            Closed = true;
            base.Close();
        }

        public void FillBuffer(StreamData data)
        {
            lock (Cache) Cache.Enqueue(data);
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
        
        private void UpdateTask()
        {
            try
            {
                if (Updating || Closed) return;
                Updating = true;
                while (CacheCount() != 0)
                {
                    StreamData? data;
                    lock (Cache) data = Cache.Dequeue();
                    var task = BackingStream.WriteAsync(data.Data).AsTask();
                    task.Start();
                    task.Wait();
                    // Ironic I know. Some streams don't support synchronized writing. Too bad!
                }

                Updating = false;
            }
            catch (Exception e)
            {
                Debug.Write($"Feedable stream update task failed: \"{e}\"");
            }
        }

        public override void Flush()
        {
            UpdateTask();
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
            FillBuffer(new StreamData
            {
                Data = buffer,
                Offset = offset,
                Count = count
            });
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