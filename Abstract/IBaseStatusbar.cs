using System.Threading.Tasks;
using BatToshoRESTApp.Enums;

namespace BatToshoRESTApp.Abstract
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