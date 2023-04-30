using DiscordBot.Methods;

namespace TestingApp.Semaphore_Tests;

public static class DelayedRelease
{
    public static async Task Test()
    {
        var task =  Task.Factory.StartNew(() => { });
        var iteration = 0;
        
        while (++iteration < 10)
        {
            var i = iteration;
            task = task.ContinueWith(async _ =>
            {
                await Task.Delay(2000);
                await Debug.WriteAsync($"Iteration {i}");
            }, CancellationToken.None).Unwrap();
        }

        await task;
        Console.WriteLine(task);
    }
}