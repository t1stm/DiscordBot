using System.Threading.Tasks;

namespace BatToshoRESTApp.Abstract
{
    public interface IBaseJson
    {
        Task<string> Read();
        Task Write(string text);
    }
}