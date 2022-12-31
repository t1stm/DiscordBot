using System;
using System.IO;
using System.Threading;
using DiscordBot.Methods;
using DiscordBot.Tools;

namespace TestingApp.StreamSpreader_Tests
{
    public static class DataAccuracy
    {
        public static void Test()
        {
            var rng = new Random();
            byte[] dummyBytes = new byte[1 << 21];
            rng.NextBytes(dummyBytes);
            var sourceStream = new MemoryStream(dummyBytes);
            var streamSpreader = new StreamSpreader(CancellationToken.None);
            MemoryStream[] memoryArray = new MemoryStream[2];
            for (var i = 0; i < memoryArray.Length; i++)
            {
                memoryArray[i] = new MemoryStream();
                streamSpreader.AddDestination(memoryArray[i]);
            }

            //streamSpreader.Write(dummyBytes, 0, dummyBytes.Length);
            sourceStream.CopyTo(streamSpreader);
            streamSpreader.Finish().Wait();
            streamSpreader.Close();
            for (var i = 0; i < memoryArray.Length; i++)
            {
                var stream = memoryArray[i];
                var data = stream.ToArray();
                Debug.Write($"Checking array: {i} with length: {data.Length}");
                for (var j = 0; j < data.Length; j++)
                {
                    if (data[j] == dummyBytes[j]) continue;
                    Debug.Write($"Wrong byte at: {j} in array: {i}");
                }
            }
        }
    }
}