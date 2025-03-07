using ConsoleApp4;
using System.Threading.Channels;

var channel = Channel.CreateUnbounded<Item>();

var producer = Task.Run(async () =>
{
    for (int i = 1; i <= 20; i++)
    {
        Console.WriteLine($"Producing {i}");
        // Send number to channel
        await channel.Writer.WriteAsync(new Item
        {
            Id = i,
            Name = $"Item {i}",
        });
        await Task.Delay(5000); // Simulate work
        Console.WriteLine($"Produced {i}");
    }
});

var consumers = Enumerable.Range(0, 3).Select(i => Task.Run(async () =>
{
   await foreach (var item in channel.Reader.ReadAllAsync())
   {
       Console.WriteLine($"Consumer {i} processing {item.Name}");
       await Task.Delay(20000); // Simulate processing
       Console.WriteLine($"Consumer {i} processed {item.Name}");
   }
})).ToList();

await producer;
channel.Writer.Complete(); // Signal consumers that no more items will be added
await Task.WhenAll(consumers);