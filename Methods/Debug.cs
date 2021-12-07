using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BatToshoRESTApp.Methods
{
    public static class Debug
    {
        public static async Task WriteAsync(string text, bool save = false)
        {
            var date = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}";
            var transformed = $"{date}: {text}";
            Console.WriteLine(transformed);
            if (!save) return;
            await File.AppendAllTextAsync($"{Bot.WorkingDirectory}/BatTosho_latest.log", transformed + "\n",
                Encoding.UTF8);
        }

        public static void Write(string text, bool save = false)
        {
            var date = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}";
            var transformed = $"{date}: {text}";
            Console.WriteLine(transformed);
            if (!save) return;
            File.AppendAllText($"{Bot.WorkingDirectory}/BatTosho_latest.log", transformed + "\n", Encoding.UTF8);
        }
    }
}