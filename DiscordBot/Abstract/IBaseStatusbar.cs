using System.Threading.Tasks;
using DiscordBot.Enums;

namespace DiscordBot.Abstract
{
    public interface IBaseStatusbar
    {
        Task UpdateStatusbar();
        Task Start();
        void Stop();
        void ChangeMode(StatusbarMode mode);
        string GenerateStatusbar();
        Task UpdatePlacement();
    }
}