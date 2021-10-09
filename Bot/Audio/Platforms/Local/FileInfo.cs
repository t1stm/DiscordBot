using System;
using System.Linq;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;
using TagLib;

namespace Bat_Tosho.Audio.Platforms.Local
{
    public class FileInfo
    {
        public VideoInformation GetInfo(string filepath, DiscordUser user, bool generateTitle)
        {
            filepath = filepath[0] switch
            {
                '/' => filepath[1] switch {'/' => filepath[1..], _ => filepath}, _ => filepath
            };
            string name, author;
            int length;
            var genAuthor = filepath.Split("-").First().Replace("_", " ").Trim();
            var genTitle = filepath.Split("-").Last().Split(".").First().Replace("_", " ").Trim();
            try
            {
                var tagFile = File.Create(filepath);
                name = string.IsNullOrEmpty(tagFile.Tag.Title) switch
                {
                    true => generateTitle switch {true => genTitle, false => filepath}, _ => tagFile.Tag.Title
                };
                author = string.IsNullOrEmpty(tagFile.Tag.JoinedPerformers) switch
                {
                    true => generateTitle switch {true => genAuthor, false => ""}, _ => tagFile.Tag.JoinedPerformers
                };
                length = (int) tagFile.Properties.Duration.TotalMilliseconds;
            }
            catch (Exception)
            {
                name = genTitle;
                author = genAuthor;
                length = -1;
            }

            return new VideoInformation(filepath, VideoSearchTypes.Downloaded, PartOf.LocalFile, name, author, length,
                user);
        }
    }
}