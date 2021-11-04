namespace BatToshoRESTApp.Readers
{
    public interface IBaseJson
    {
        string Read();
        void Write(string text);
    }
}