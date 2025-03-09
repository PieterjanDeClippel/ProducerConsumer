using ConsoleApp4;
using System.Threading.Channels;
using static Confluent.Kafka.ConfigPropertyNames;


var store = new ItemStore();


var channelA = Channel.CreateUnbounded<ItemsResponse>();
var channelB = Channel.CreateUnbounded<ItemsResponse>();

var pipelineA = Pipeline<ItemsResponse>.Create(async (pageNumber, consumerId) =>
{
    ConsoleEx.WriteLine($"[A] Fetching page {pageNumber}");
    var items = await store.GetItems(pageNumber * 25, 25);
    await channelA.Writer.WriteAsync(items);
    ConsoleEx.WriteLine($"""
        [A] Fetched page {pageNumber}
            {string.Join(',', items.Items.Select(x => x.Name))}
        """);

    return items.HasMore;
});

var pipelineB = Pipeline<ItemsResponse>.Create(async (pageNumber, consumerId) =>
{
    await foreach (var page in channelA.Reader.ReadAllAsync())
    {
        ConsoleEx.WriteLine($"[B] Reversing {page.PageStart} to {page.PageEnd} (Consumer {consumerId})");
        await Task.Delay(5000); // Simulate processing
        var reversed = new ItemsResponse
        {
            PageStart = page.PageStart,
            HasMore = page.HasMore,
            Items = page.Items.Select(x => new Item
            {
                Id = x.Id,
                Name = new string(x.Name.Reverse().ToArray()),
            }).ToArray(),
        };
        await channelB.Writer.WriteAsync(reversed);
        ConsoleEx.WriteLine($"""
            [B] Reversed {reversed.PageStart} to {reversed.PageEnd} (Consumer {consumerId})
                {string.Join(',', reversed.Items.Select(x => x.Name))}
            """);

    }
    return false;
}, 3);

var pipelineC = Pipeline<ItemsResponse>.Create(async (pageNumber, consumerId) =>
{
    await foreach (var page in channelB.Reader.ReadAllAsync())
    {
        ConsoleEx.WriteLine($"[C] Capsing {page.PageStart} to {page.PageEnd} (Consumer {consumerId})");
        await Task.Delay(6000); // Simulate processing
        var capsed = new ItemsResponse
        {
            PageStart = page.PageStart,
            HasMore = page.HasMore,
            Items = page.Items.Select(x => new Item
            {
                Id = x.Id,
                Name = new string(x.Name.ToUpper()),
            }).ToArray(),
        };
        ConsoleEx.WriteLine($"""
            [C] Capsed {capsed.PageStart} to {capsed.PageEnd} (Consumer {consumerId})
                {string.Join(',', capsed.Items.Select(x => x.Name))}
            """);

    }
    return false;
}, 3);


await pipelineA;
channelA.Writer.Complete(); // Signal consumers that no more items will be added
await pipelineB;
channelB.Writer.Complete(); // Signal consumers that no more items will be added
await pipelineC;