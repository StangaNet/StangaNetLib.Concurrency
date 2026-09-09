using System.Runtime.CompilerServices;
using System.Threading.Channels;
using StangaNetLib.Core.Common;
using StangaNetLib.Concurrency.Configuration;
using Microsoft.Extensions.Options;
using StangaNetLib.Concurrency.Core;

namespace StangaNetLib.Concurrency.Synchronization;

/// <summary>
/// Bounded producer-consumer queue backed by <see cref="System.Threading.Channels.Channel{T}"/>.
/// Can be constructed from DI-injected <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>
/// or directly with an explicit capacity and overflow policy (used in tests and factory creation).
/// </summary>
internal sealed class BoundedWorkQueue<T> : IWorkQueue<T>, IAsyncDisposable
{
    private readonly Channel<T> _channel;

    internal BoundedWorkQueue(IOptions<ConcurrencySettings> options)
    {
        var settings = options.Value;
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(settings.WorkQueueCapacity)
        {
            FullMode = settings.WorkQueueFullMode,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    internal BoundedWorkQueue(int capacity, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
    {
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = fullMode,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    public int Count => _channel.Reader.Count;

    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    public async ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        => await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);

    public Result<T> TryEnqueue(T item)
    {
        if (_channel.Writer.TryWrite(item))
            return Result<T>.Success(item);

        if (IsCompleted)
            return Result<T>.Failure(ConcurrencyErrors.QueueCompleted);

        return Result<T>.Failure(ConcurrencyErrors.QueueFull);
    }

    public void Complete() => _channel.Writer.TryComplete();

    public async IAsyncEnumerable<T> ConsumeAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return item;
    }

    public ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
