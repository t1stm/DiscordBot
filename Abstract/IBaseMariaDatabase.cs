using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;

namespace BatToshoRESTApp.Abstract
{
    public interface IBaseMariaDatabase
    {
        Task<YoutubeVideoInformation> Read(string id);
        Task Add(YoutubeVideoInformation videoInformation);
    }
}