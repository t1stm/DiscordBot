using System.Threading.Tasks;

namespace BatToshoRESTApp.Readers
{
    public interface IBaseJson
    {
        Task<string> Read();
        Task Write(string text);
    }
}