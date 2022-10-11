#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomPlaylistFormat;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Methods;

namespace DiscordBot.Playlists
{
    public static class PlaylistManager
    {
        public const string PlaylistDirectory = $"{Bot.WorkingDirectory}/NewPlaylists";
        
        public static Playlist? GetIfExists(Guid guid)
        {
            var filename = $"{PlaylistDirectory}/{guid.ToString()}.play";
            if (!File.Exists(filename)) return null;
            
            var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var decoder = new Decoder(file);
            var read = decoder.Read();
            if (read.Info != null)
                read.Info.Guid = guid;
            return read;
        }

        public static Playlist? SavePlaylist(IEnumerable<PlayableItem> list, PlaylistInfo info)
        {
            var guid = Guid.NewGuid();
            var fileStream = File.Open($"{PlaylistDirectory}/{guid.ToString()}.play", FileMode.CreateNew);
            try
            {
                var playlistEncoder = new Encoder(fileStream, info);

                var entries = list.Select(r => new Entry
                {
                    Type = GetItemType(r),
                    Data = string.Join("://", r.GetAddUrl().Split("://")[1..])
                });

                var items = entries as Entry[] ?? entries.ToArray();
                playlistEncoder.Encode(items);
                info.Guid = guid;
                return new Playlist
                {
                    FailedToParse = false,
                    PlaylistItems = items,
                    Info = info
                };
            }
            catch (Exception e)
            {
                Debug.Write($"Save Playlist failed: \"{e}\"");
            }
            finally
            {
                fileStream.Close();
            }

            return null;
        }
        
        public static Playlist SavePlaylist(IEnumerable<PlayableItem> list, PlaylistInfo info, Guid guid)
        {
            var fileStream = File.Open($"{PlaylistDirectory}/{guid.ToString()}.play", FileMode.Create);
            var playlistEncoder = new Encoder(fileStream, info);

            var entries = list.Select(r => new Entry
            {
                Type = GetItemType(r),
                Data = string.Join("://", r.GetAddUrl().Split("://")[1..])
            });

            var items = entries as Entry[] ?? entries.ToArray();
            playlistEncoder.Encode(items);
            info.Guid = guid;
            return new Playlist
            {
                FailedToParse = false,
                PlaylistItems = items,
                Info = info
            };
        }

        public static byte GetItemType(PlayableItem it) => it switch
        {
            YoutubeVideoInformation => 01,
            SpotifyTrack => 02,
            SystemFile => 03,
            Vbox7Video => 05,
            OnlineFile => 06,
            TtsText => 07,
            TwitchLiveStream => 08,
            YoutubeOverride => 09,
            _ => 255
        };
    }
}