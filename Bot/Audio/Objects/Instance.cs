using System.Collections.Generic;
using System.Linq;
using Bat_Tosho.Enums;
using Bat_Tosho.Messages;
using Bat_Tosho.Methods;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace Bat_Tosho.Audio.Objects
{
    public class Instance
    {
        public List<VideoInformation> VideoInfos = new();
        public Ffmpeg Ffmpeg { get; set; }
        public DiscordMessage StatusbarMessage { get; set; }
        public DiscordChannel VoiceChannel { get; set; }
        public int Song { get; set; }
        public bool Playing { get; set; }
        public Statusbar Statusbar { get; set; }
        public bool UpdatingLists { get; set; }
        public int ActiveDownloadTasks { get; set; }
        public VoiceTransmitSink TransmitSink { get; set; }
        public Player Player { get; set; }

        public LoopStatus LoopStatus { get; set; } = LoopStatus.None;

        public bool WaitingToLeave { get; set; }
        
        public ServerSettings ServerSettings { get; set; }

        public DiscordChannel StatusbarChannel { get; set; }

        public VideoInformation CurrentVideoInfo()
        {
            return Song < 0
                ? new VideoInformation("", VideoSearchTypes.Downloaded, PartOf.None, "Placeholder Videoinformation.",
                    "Something wrong has happened with the bot and this is a measure not to crash it.")
                : VideoInfos.ElementAt(Song);
        }
    }
}