namespace DiscordBot.Data.Models
{
    public class GuildsModel
    {
        public ulong Id { get; set; }
        public ushort Language { get; set; }
        public ushort Statusbar { get; set; }
        public bool VerboseMessages { get; set; }
        public bool Normalize { get; set; }
        public bool ShowOriginalInfo { get; set; }
        public bool SaveQueueOnLeave { get; set; }
    }
}