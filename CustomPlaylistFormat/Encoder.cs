#nullable enable
using System.Text;
using CustomPlaylistFormat.Objects;

namespace CustomPlaylistFormat;

public class Encoder
{
    private readonly PlaylistInfo? Info;
    private readonly BinaryWriter Writer;

    private bool Started;

    public Encoder(Stream backingStream, PlaylistInfo? info = null)
    {
        Info = info;
        Writer = new BinaryWriter(backingStream);
    }

    public void Encode(IEnumerable<Entry> data)
    {
        var list = data.ToList();
        if (list.Count < 1)
            throw new InvalidDataException($"Given data count is less than one item. {nameof(data)}: {list.Count}");
        if (!Started)
        {
            Started = true;
            AddFileStartHeader();
            AddInfo(list.Count);
            AddPlaylistBeginHeader();
        }

        foreach (var item in list) EncodePart(item);
    }

    private void AddFileStartHeader()
    {
        Writer.Write(FormatConstants.FileStartHeader);
    }

    private void AddPlaylistBeginHeader()
    {
        Writer.Write(FormatConstants.PlaylistBeginHeader);
    }

    private void AddInfo(int listCount)
    {
        Queue<byte[]> writeQueue = new();
        Writer.Write(FormatConstants.InfoBeginHeader);
        byte position = 0;
        byte features = 0;
        if (Info?.Maker != null)
        {
            features = SetBit(features, position);
            AddToQueue(writeQueue, Info.Maker);
        }

        position++;
        if (Info?.Name != null)
        {
            features = SetBit(features, position);
            AddToQueue(writeQueue, Info.Name);
        }

        position++;
        if (Info?.Description != null)
        {
            features = SetBit(features, position);
            AddToQueue(writeQueue, Info.Description);
        }

        position++;
        if (Info?.IsPublic ?? true) features = SetBit(features, position);
        position++;
        if (listCount > ushort.MaxValue)
        {
            features = SetBit(features, position);
            AddToQueue(writeQueue, (uint)listCount);
        }
        else
        {
            AddToQueue(writeQueue, (ushort)listCount);
        }

        AddToQueue(writeQueue, Info?.LastModified ?? 0);
        Writer.Write(features);
        while (writeQueue.Count != 0) Writer.Write(writeQueue.Dequeue());
    }

    private void EncodePart(Entry data)
    {
        var urlBytes = Encoding.UTF8.GetBytes(data.Data);
        Writer.Write((ushort)urlBytes.Length);
        Writer.Write(data.Type);
        Writer.Write(urlBytes);
    }

    #region Data Modifiers

    private static void AddToQueue(Queue<byte[]> queue, string info)
    {
        var text = Encoding.UTF8.GetBytes(info);
        queue.Enqueue(new[] { (byte)text.Length });
        queue.Enqueue(text.Length > byte.MaxValue ? text[..byte.MaxValue] : text);
    }

    private static void AddToQueue(Queue<byte[]> queue, long info)
    {
        queue.Enqueue(BitConverter.GetBytes(info));
    }

    private static void AddToQueue(Queue<byte[]> queue, ushort info)
    {
        queue.Enqueue(BitConverter.GetBytes(info));
    }

    private static void AddToQueue(Queue<byte[]> queue, uint info)
    {
        queue.Enqueue(BitConverter.GetBytes(info));
    }

    private static byte SetBit(byte startingByte, byte position)
    {
        return (byte)(startingByte | (1 << position));
    }

    #endregion
}