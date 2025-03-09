using ConsoleApp4;
using System.Threading.Channels;


var store = new ItemStore();


var channelA = Channel.CreateUnbounded<ItemsResponse>();
var channelB = Channel.CreateUnbounded<ItemsResponse>();

var producerA = Enumerable.Range(0, 1)
    .Select(_ => Task.Run(async () =>
    {
        var hasMore = true;
        var i = 0;
        do
        {
            ConsoleEx.WriteLine($"[A] Fetching page {i}");
            var items = await store.GetItems(i * 25, 25);
            await channelA.Writer.WriteAsync(items);
            ConsoleEx.WriteLine($"""
                [A] Fetched page {i}
                    {string.Join(',', items.Items.Select(x => x.Name))}
                """);
            
            hasMore = items.HasMore;
            i++;
        }
        while (hasMore);
    }));

var consumersA = Enumerable.Range(0, 3)
    .Select(consumerId => Task.Run(async () =>
    {
        var hasMore = true;
        var i = 0;
        do
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

            hasMore = false;
            i++;
        }
        while (hasMore);
    }))
    .ToList();


var consumersB = Enumerable.Range(0, 3)
    .Select(consumerId => Task.Run(async () =>
    {
        var hasMore = true;
        var i = 0;
        do
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

            hasMore = false;
            i++;
        }
        while (hasMore);
    }))
    .ToList();


await Task.WhenAll(producerA);
channelA.Writer.Complete(); // Signal consumers that no more items will be added
await Task.WhenAll(consumersA);
channelB.Writer.Complete(); // Signal consumers that no more items will be added
await Task.WhenAll(consumersB);