using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bat_Tosho
{
    public class Debug
    {
        public static async Task Write(string text, bool save = true)
        {
            var date = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}";
            var transformed = $"{date}: {text}";
            Console.WriteLine(transformed);
            switch (save)
            {
                case true:
                    await File.AppendAllTextAsync($"{Program.MainDirectory}BatTosho_latest.log", transformed + "\n",
                        Encoding.UTF8);
                    break;
                case false: return;
            }
        }

        public static string WriteAndReturnString(string text, string debugMessage, bool save = true)
        {
            var date = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}";
            var transformed = $"{date}: {debugMessage} - {text}";
            Console.WriteLine(transformed);
            switch (save)
            {
                case true:
                    File.AppendAllText($"{Program.MainDirectory}BatTosho_latest.log", transformed + "\n",
                        Encoding.UTF8);
                    break;
                case false: return text;
            }

            return text;
        }
    }
}