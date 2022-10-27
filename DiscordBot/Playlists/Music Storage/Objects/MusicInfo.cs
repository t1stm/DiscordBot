using System.Text.Json.Serialization;

#nullable enable
namespace DiscordBot.Playlists.Music_Storage.Objects
{
    public class MusicInfo
    {
        [JsonInclude, JsonPropertyName("titleOriginal")]
        public string? OriginalTitle;
        [JsonInclude, JsonPropertyName("titleRomanized")]
        public string? RomanizedTitle;
        [JsonInclude, JsonPropertyName("authorOriginal")]
        public string? OriginalAuthor;
        [JsonInclude, JsonPropertyName("authorRomanized")]
        public string? RomanizedAuthor;
        [JsonInclude, JsonPropertyName("location")]
        public string RelativeLocation = null!;
        [JsonInclude, JsonPropertyName("id")]
        private string? Id;
        [JsonInclude, JsonPropertyName("length")]
        public ulong Length;

        public MusicInfo()
        {
            RomanizedTitle ??= OriginalTitle;
            RomanizedAuthor ??= OriginalAuthor;
        }
        
        public string AddId => Id ??= GenerateRandomId();
        private string GenerateRandomId()
        {
            return $"{RomanizedAuthor?[..2].ToLower()}{RomanizedTitle?[..2].ToLower()}{RomanizedTitle?[..6].ToLower()}-{Bot.RandomString(2)}";
        }

        public bool IsTitleInEnglish => RomanizedTitle == null && RomanizedAuthor == null;
    }
}