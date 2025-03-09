using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ConsoleApp4;

public abstract class Pipeline : IDisposable
{
    protected internal Pipeline? inner;

    public abstract void Dispose();
}

public class Pipeline<Tout> : Pipeline<object, Tout>
{
    public static Pipeline<object, Tout> Create(Func<int, int, Channel<Tout>, Task<bool>> action, int consumerCount = 1)
    {
        var result = Pipeline<object, Tout>.Create(
            (pageNumber, consumerId, _, output) => action(pageNumber, consumerCount, output),
            null,
            consumerCount);
        return result;
    }

    public override Pipeline<Tout, Tout2> Concat<Tout2>(Func<int, int, Channel<Tout>, Channel<Tout2>, Task<bool>> action, int consumerCount = 1)
    {
        var result = Pipeline<Tout, Tout2>.Create(
            (pageNumber, consumerId, input, output) => action(pageNumber, consumerCount, input!, output),
            output, // This output is the input of the next pipeline
            consumerCount);

        result.inner = this;

        return result;
    }

    internal Pipeline(Func<int, int, Channel<Tout>, Task<bool>> action, Channel<object>? input, int consumerCount) : base((pageNumber, consumerId, input, output) => action(pageNumber, consumerId, output), input, consumerCount)
    {
    }
}

public class Pipeline<Tin, Tout> : Pipeline
{
    protected readonly Channel<Tout> output = Channel.CreateUnbounded<Tout>();
    protected readonly Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action;
    protected readonly Channel<Tin>? input;
    protected readonly int consumerCount;

    private readonly Task[] tasks;

    protected Pipeline(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, Channel<Tin>? input, int consumerCount)
    {
        this.action = action;
        this.input = input;
        this.consumerCount = consumerCount;
        this.tasks = Enumerable.Range(0, consumerCount)
            .Select(consumerId => Task.Run(async () =>
            {
                var hasMore = true;
                var i = 0;
                do
                {
                    hasMore = await action(i, consumerId, input, output);
                    i++;
                }
                while (hasMore);
            }))
            .ToArray();
    }

    //public static Pipeline<TOUT> Create<TOUT>(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, int consumerCount = 1)
    //{
    //    return Create<Tin, Tout>()
    //}

    internal static Pipeline<Tin, Tout> Create(Func<int, int, Channel<Tin>?, Channel<Tout>, Task<bool>> action, Channel<Tin>? input = null, int consumerCount = 1)
    {
        return new Pipeline<Tin, Tout>(action, input, consumerCount);
    }

    public virtual Pipeline<Tout, Tout2> Concat<Tout2>(Func<int, int, Channel<Tout>, Channel<Tout2>, Task<bool>> action, int consumerCount = 1)
    {
        var result = Pipeline<Tout, Tout2>.Create(
            (pageNumber, consumerId, input, output) => action(pageNumber, consumerCount, input!, output),
            output, // This output is the input of the next pipeline
            consumerCount);
        result.inner = this;
        return result;
    }

    public override void Dispose()
    {
        output.Writer.Complete();
    }

    public TaskAwaiter GetAwaiter()
    {
        return Task.WhenAll(tasks).GetAwaiter();
    }
}