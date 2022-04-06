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
            var runType = Bot.RunType.Beta;
            if (args.Length > 0)
                if (args[0] == "release")
                    runType = Bot.RunType.Release;
            var botTask = new Task(async () => { await Bot.Initialize(runType); });
            botTask.Start();
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => { logging.ClearProviders(); }).ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}