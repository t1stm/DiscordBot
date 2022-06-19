using System.Threading.Tasks;

namespace DiscordBot.Abstract
{
    public interface IBaseJson
    {
        Task<string> Read();
        Task Write(string text);
    }
}