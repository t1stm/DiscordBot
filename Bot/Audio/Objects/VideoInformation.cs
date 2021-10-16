using System;
using System.Diagnostics;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;

namespace Bat_Tosho.Audio.Objects
{
    public class VideoInformation
    {
        public readonly string YoutubeIdOrPathToFile;

        public TimeSpan Length;

        public VideoInformation(string youtubeIdOrPathToFile, VideoSearchTypes type, PartOf partOf, string name = null,
            string author = null,
            int lengthMs = 0, DiscordUser requester = null, DiscordUser controller = null, string thubmnailUrl = null)
        {
            YoutubeIdOrPathToFile = youtubeIdOrPathToFile;
            Type = type;
            PartOf = partOf;
            ThubmnailUrl = thubmnailUrl;
            Name = name;
            Author = author;
            LengthMs = lengthMs;
            Requester = requester;
            Length = TimeSpan.FromMilliseconds(LengthMs);
            Location = type switch
            {
                VideoSearchTypes.Downloaded or VideoSearchTypes.HttpFileStream => YoutubeIdOrPathToFile, _ => null
            };
        }

        public string Location { get; set; }
        public VideoSearchTypes Type { get; set; }

        public PartOf PartOf { get; set; }
        public string Name { get; }

        public string Author { get; }

        public int LengthMs { get; }

        public Stopwatch Stopwatch { get; } = new();
        public bool Paused { get; set; }

        public DiscordUser Requester { get; }

        public string ThubmnailUrl { get; init; }
        public bool Lock { get; set; }
    }
}