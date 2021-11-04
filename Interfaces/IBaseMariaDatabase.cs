using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;

namespace BatToshoRESTApp.Readers.MariaDB
{
    public interface IBaseMariaDatabase
    {
        Task<YoutubeVideoInformation> Read(string id);
        Task Add(YoutubeVideoInformation videoInformation);
    }
}