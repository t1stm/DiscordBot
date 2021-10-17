using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BatToshoRESTApp
{
    public static class Program
    {
        //EEEEE HABIBI GAZARI KAK STE
        //ETO VI LINKA ZA DISCORD TESTING SERVERA: https://discord.gg/fZ74marh7R
        public static void Main(string[] args)
        {
            var release = false;
            if (args.Length > 0)
                if (args[0] == "release")
                    release = true;
            var botTask = new Task(async () => { await Bat_Tosho.Program.MainAsync(release); });
            botTask.Start();
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}