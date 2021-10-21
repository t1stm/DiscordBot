using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Audio.Platforms.Discord;
using Bat_Tosho.Audio.Platforms.Local;
using Bat_Tosho.Audio.Platforms.Spotify;
using Bat_Tosho.Audio.Platforms.Youtube;
using Bat_Tosho.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Playlist = Bat_Tosho.Audio.Platforms.Spotify.Playlist;

namespace Bat_Tosho.Audio.Platforms
{
    public class Search
    {
        public Search(CommandContext context)
        {
            Context = context;
            MessageAttachments = context.Message.Attachments;
            User = context.User;
        }

        private IReadOnlyList<DiscordAttachment> MessageAttachments { get; }
        private DiscordUser User { get; }
        private CommandContext Context { get; }

        public async Task<List<VideoInformation>> GetResults(string path, PartOf partOf = PartOf.None, int lengthMs = 0)
        {
            path = path.Trim();
            if (Context.Message.MentionedUsers.Count >= 1)
                return new List<VideoInformation>
                {
                    new(null, VideoSearchTypes.SpotifyListenAlong, PartOf.SpotifyListenAlong, null,
                        null, -1, User, Context.Message.MentionedUsers[0])
                };

            var folders = new Folders();
            var search = new SearchResult(User);
            //Discord Attachment Downloader
            if (MessageAttachments.Count >= 1)
            {
                await Debug.Write("Message has attachments.");
                List<VideoInformation> vi = new();
                foreach (var attachment in MessageAttachments)
                    vi.Add(await Attachment.Download(attachment, User, Context.Guild.Id));

                return vi;
            }

            //Shameless plugs.
            if (path.ToLower().StartsWith("zhik tak") || path.ToLower().StartsWith("жик так"))
                path = "https://www.youtube.com/watch?v=gm_aFB_7kmc";
            //Custom Playlists Time.
            if (path.ToLower().StartsWith("2:00 pod masata") || path.ToLower().StartsWith("2:00 под масата"))
                path = "https://www.youtube.com/playlist?list=PLYfMO6M2P5pAibyUXN9XFP6zbG0kL0iYe";

            if (path.ToLower().StartsWith("bat tosho song collection"))
                return folders.GetFolder("/home/kris/Music", User, true);

            if (path.Contains("youtu")) //Youtube Stuff
            {
                await Debug.Write("Path is Youtube Url");
                if (path.Contains("playlist?list="))
                    return await search.Get(path, VideoSearchTypes.YoutubePlaylist, PartOf.YoutubePlaylist);

                if (path.Contains(@"?v=") || path.Contains("shorts/") || path.Contains("youtu.be"))
                    return await search.Get(path, VideoSearchTypes.YoutubeVideoId, PartOf.YoutubeSearch);
            }

            if (path.Contains("open.spotify.com")) // Spotify Stuff
            {
                List<VideoInformation> list = new();
                if (path.Contains("/playlist"))
                {
                    foreach (var spotifyTrack in await Playlist.Get(path))
                    {
                        await Debug.Write($"Adding: \"{spotifyTrack.SearchTerm}\".");
                        var searchTerm = $"{spotifyTrack.SearchTerm} - Topic";
                        //Here Begin the If-Elses
                        if (searchTerm.Contains("Remaster"))
                        {
                            searchTerm = Regex.Replace(searchTerm, @"\((.*?)\)", "");
                            searchTerm = searchTerm.Replace("Remaster", "");
                        }

                        if (searchTerm.Contains("Hotel California") && searchTerm.Contains("2013"))
                            searchTerm = "Eagles - Hotel California";
                        // Fuck the seven different versions of the same song. Like come on what is wrong with That. NOTHING RIGHT?
                        if (searchTerm.Contains("Alone Again") && searchTerm.Contains("Gilbert"))
                            searchTerm = "Gilbert O'Sullivan - Alone Again (original version";
                        if (searchTerm.Contains("7\" Mix")) searchTerm = searchTerm.Replace("7\" Mix", "");

                        var result = new VideoInformation(searchTerm, VideoSearchTypes.SearchTerm,
                            PartOf.SpotifyPlaylist,
                            spotifyTrack.TrackName, spotifyTrack.ArtistsCombined, spotifyTrack.LengthMs, User);
                        list.Add(result);
                    }

                    return list;
                }

                if (!path.Contains("/track")) return await DefaultReturn(path, partOf);
                var track = await Track.Get(path);
                return await search.Get(track.SearchTerm, VideoSearchTypes.SearchTerm, PartOf.SpotifyTrack, lengthMs);
            }

            if (path.StartsWith("file:///"))
                return folders.GetFolder(path[7..], User, false);
            if (path.StartsWith("http://") || path.StartsWith("https://")) //Http File Stream
                return new List<VideoInformation>
                {
                    new(path, VideoSearchTypes.HttpFileStream, PartOf.HttpFileStream, path,
                        "HTTP File Stream", -1, User)
                };

            return await DefaultReturn(path, partOf);
        }

        private async Task<List<VideoInformation>> DefaultReturn(string path, PartOf partOf = PartOf.None)
        {
            var search = new SearchResult(User);
            return await search.Get(path, VideoSearchTypes.SearchTerm,
                partOf switch {PartOf.None => PartOf.YoutubeSearch, _ => partOf});
        }
    }
}