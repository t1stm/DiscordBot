namespace DiscordBot.Data.Models
{
    public class UsersModel

    {
    public ulong Id { get; set; }
    public string Token { get; set; }
    public ushort Language { get; set; }
    public bool UiScroll { get; set; }
    public bool ForceUiScroll { get; set; }
    public bool LowSpec { get; set; }
    }
}