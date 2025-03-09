using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ConsoleApp4;

public class Pipeline<Tout>
{
    public static Pipeline<Tout> Create(Func<int, int, Channel<Tout>, Task<bool>> action, int consumerCount = 1)
    {
        var result = Pipeline<object, Tout>.Create(
            (pageNumber, consumerId, _, output) => action(pageNumber, consumerCount, output),
            null,
            consumerCount);
        return result;
    }

    public virtual Pipeline<Tout, Tout2> Concat<Tout2>(Func<int, int, Channel<Tout>, Channel<Tout2>, Task<bool>> action, int consumerCount = 1)
    {
        var result = Pipeline<Tout, Tout2>.Create(
            (pageNumber, consumerId, input, output) => action(pageNumber, consumerCount, input!, output),
            output, // This output is the input of the next pipeline
            consumerCount);
        return result;
    }

    protected Pipeline(Func<int, int, Channel<Tout>, Task<bool>> action, int consumerCount)
    {
        //this.tasks = tasks;
        this.tasks = Enumerable.Range(0, consumerCount)
            .Select(consumerId => Task.Run(async () =>
            {
                var hasMore = true;
                var i = 0;
                do
                {
                    hasMore = await action(i, consumerId, output);
                    i++;
                }
                while (hasMore);
            }))
            .ToArray();
    }

    protected readonly Channel<Tout> output = Channel.CreateUnbounded<Tout>();
    private readonly Task[] tasks;

    public TaskAwaiter GetAwaiter()
    {
        output.Writer.Complete();
        return Task.WhenAll(tasks).GetAwaiter();
    }
}

public class Pipeline<Tin, Tout> : Pipeline<Tout>
{
    public Pipeline(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, int consumerCount) : base((pageNumber, consumerId, output) => action(pageNumber, consumerId, null, output), consumerCount) { }

    //public static Pipeline<TOUT> Create<TOUT>(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, int consumerCount = 1)
    //{
    //    return Create<Tin, Tout>()
    //}

    internal static Pipeline<Tin, Tout> Create(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, Channel<Tin>? input = null, int consumerCount = 1)
    {
        //var result = new Pipeline<Tin, Tout>(
        //    Enumerable.Range(0, consumerCount)
        //        .Select(consumerId => Task.Run(async () =>
        //        {
        //            var hasMore = true;
        //            var i = 0;
        //            do
        //            {
        //                hasMore = await action(i, consumerId, input, output);
        //                i++;
        //            }
        //            while (hasMore);
        //        }))
        //        .ToArray());
        var result = new Pipeline<Tin, Tout>(action, consumerCount);
        return result;
    }

    public override Pipeline<Tout, Tout2> Concat<Tout2>(Func<int, int, Channel<Tout>, Channel<Tout2>, Task<bool>> action, int consumerCount = 1)
    {
        var t = base.Concat(action, consumerCount);
        return t;
    }
}