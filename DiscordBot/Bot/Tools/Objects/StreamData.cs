namespace DiscordBot.Tools.Objects
{
    public class StreamData
    {
        public byte[] Data { get; init; } = null!;
        public int Offset;
        public int Count;
    }
}