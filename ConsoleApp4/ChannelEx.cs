using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ConsoleApp4;

public static class ChannelEx
{
    public static void Pipeline<T>(this Channel<T> channel)
    {

    }
}

public class Pipeline<T>
{
    public static Pipeline<T> Create(Func<int, int, Task<bool>> action, int consumerCount = 1)
    {
        var result = new Pipeline<T>
        {
            tasks = Enumerable.Range(0, consumerCount)
                .Select(consumerId => Task.Run(async () =>
                {
                    var hasMore = true;
                    var i = 0;
                    do
                    {
                        hasMore = await action(i, consumerId);
                        i++;
                    }
                    while (hasMore);
                }))
                .ToArray(),
        };
        return result;
    }

    private readonly Channel<T> channel = Channel.CreateUnbounded<T>();
    private IEnumerable<Task> tasks = [];

    public TaskAwaiter GetAwaiter()
        => Task.WhenAll(tasks).GetAwaiter();
}