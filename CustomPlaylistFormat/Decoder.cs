#nullable enable
using System.Text;
using CustomPlaylistFormat.Objects;

namespace CustomPlaylistFormat;

public enum ReadMode
{
    Whole,
    InfoOnly
}

public class Decoder
{
    private readonly BinaryReader Reader;

    public Decoder(Stream fileStream)
    {
        Reader = new BinaryReader(fileStream);
    }

    public Playlist Read(ReadMode readMode = ReadMode.Whole)
    {
        var playlist = new Playlist
        {
            FailedToParse = !VerifyTag(FormatConstants.FileStartHeader)
        };

        if (playlist.FailedToParse) return playlist;

        Span<byte> tag = stackalloc byte[4];
        Reader.Read(tag);

        if (ArraysEqual(tag, FormatConstants.InfoBeginHeader))
        {
            playlist.Info = ReadInfo();
            Reader.Read(tag);
            if (readMode == ReadMode.InfoOnly) return playlist;
        }

        if (ArraysEqual(tag, FormatConstants.PlaylistBeginHeader)) return ReadItems(playlist);

        playlist.FailedToParse = true;
        return playlist;
    }

    private PlaylistInfo ReadInfo()
    {
        var info = new PlaylistInfo();
        var features = (byte)(ReadByte() << 4);
        bool hasMaker = (features & 1) == 0,
            hasName = (features & 2) == 0,
            hasDescription = (features & 4) == 0,
            isCountInt = (features & 16) == 0;
        info.IsPublic = (features & 8) == 0;

        if (hasMaker) info.Maker = ReadString(ReadByte());
        if (hasName) info.Name = ReadString(ReadByte());
        if (hasDescription) info.Description = ReadString(ReadByte());
        info.Count = isCountInt ? ReadUInt() : ReadUShort();
        info.LastModified = ReadLong();
        return info;
    }

    private Playlist ReadItems(Playlist playlist)
    {
        List<Entry> playlistItems = new();

        while (Reader.PeekChar() != -1)
        {
            var length = Reader.ReadInt16();
            var type = ReadByte();
            var data = ReadString(length);
            playlistItems.Add(new Entry
            {
                Type = type,
                Data = data
            });
        }

        playlist.PlaylistItems = playlistItems.ToArray();
        return playlist;
    }

    private unsafe bool VerifyTag(IReadOnlyList<char> header)
    {
        Span<byte> data = stackalloc byte[8];
        var read = Reader.Read(data);
        return read == data.Length && ArraysEqual(data, header);
    }

    private static bool ArraysEqual(Span<byte> arr1, IReadOnlyList<char> arr2)
    {
        if (arr1.Length != arr2.Count) return false;
        for (var i = 0; i < arr1.Length; i++)
            if ((char)arr1[i] != arr2[i])
                return false;
        return true;
    }

    #region Read Methods

    private byte ReadByte()
    {
        var oneByte = new byte[1];
        var readCount = Reader.Read(oneByte);
        if (readCount < 1) throw new InvalidDataException("Reading Playlist Stream: Reading one byte failed.");
        return oneByte[0];
    }

    private ushort ReadUShort()
    {
        var bytes = new byte[2];
        var readCount = Reader.Read(bytes);
        if (readCount < 2) throw new InvalidDataException("Reading Playlist Stream: Reading ushort failed.");
        return BitConverter.ToUInt16(bytes, 0);
    }

    private uint ReadUInt()
    {
        var bytes = new byte[4];
        var readCount = Reader.Read(bytes);
        if (readCount < 4) throw new InvalidDataException("Reading Playlist Stream: Reading uint failed.");
        return BitConverter.ToUInt16(bytes, 0);
    }

    private long ReadLong()
    {
        var bytes = new byte[8];
        var readCount = Reader.Read(bytes);
        if (readCount < 8) throw new InvalidDataException("Reading Playlist Stream: Reading short failed.");
        return BitConverter.ToInt64(bytes, 0);
    }

    private string ReadString(int bytes)
    {
        Span<byte> readData = stackalloc byte[bytes];
        var readCount = Reader.Read(readData);
        if (readCount != bytes)
            throw new InvalidDataException(
                $"Reading Playlist Stream: Variable \"{nameof(readCount)}\" value is not equal to the number of bytes to be read. ReadCount: {readCount}, Bytes: {bytes}");
        return Encoding.UTF8.GetString(readData);
    }

    #endregion
}