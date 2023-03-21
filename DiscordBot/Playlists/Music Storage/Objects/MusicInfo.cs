using System.Text.Json.Serialization;
using DiscordBot.Audio.Objects;

#nullable enable
namespace DiscordBot.Playlists.Music_Storage.Objects
{
    public class MusicInfo
    {
        [JsonInclude] [JsonPropertyName("coverUrl")]
        public string? CoverUrl;

        [JsonInclude] [JsonPropertyName("id")] public string? Id;

        [JsonInclude] [JsonPropertyName("length")]
        public ulong Length;

        [JsonInclude] [JsonPropertyName("authorOriginal")]
        public string? OriginalAuthor;

        [JsonInclude] [JsonPropertyName("titleOriginal")]
        public string? OriginalTitle;

        [JsonInclude] [JsonPropertyName("location")]
        public string? RelativeLocation;

        [JsonInclude] [JsonPropertyName("authorRomanized")]
        public string? RomanizedAuthor;

        [JsonInclude] [JsonPropertyName("titleRomanized")]
        public string? RomanizedTitle;

        public MusicInfo()
        {
            RomanizedTitle ??= OriginalTitle;
            RomanizedAuthor ??= OriginalAuthor;
        }

        [JsonIgnore] public bool IsTitleInEnglish => RomanizedTitle == null && RomanizedAuthor == null;

        public string GenerateRandomId()
        {
            return $"{(RomanizedAuthor?.Length > 2 ? RomanizedAuthor?[..2] : RomanizedAuthor)?.ToLower()}" +
                   $"{(RomanizedTitle?.Length > 6 ? RomanizedTitle?[..6] : RomanizedTitle + new string('0', 6 - RomanizedTitle?.Length ?? 0))?.ToLower().Replace(' ', '-')}-{Bot.RandomString(2)}";
        }

        public void UpdateRandomId(bool force = false)
        {
            Id = force || Id == null ? GenerateRandomId() : Id;
        }

        public override string ToString()
        {
            return
                $"\"{OriginalTitle} - {OriginalAuthor}\":\'{RomanizedTitle} - {RomanizedAuthor}\':\"{RelativeLocation}\"";
        }

        public MusicObject ToMusicObject()
        {
            return new()
            {
                RomanizedTitle = RomanizedTitle,
                RomanizedAuthor = RomanizedAuthor,
                RelativeLocation = RelativeLocation,
                AddId = Id,
                Title = OriginalTitle,
                Author = OriginalAuthor,
                Length = Length,
                CoverUrl = CoverUrl
            };
        }
    }
}