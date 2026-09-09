using StangaNetLib.Core.Common;

namespace StangaNetLib.Concurrency.Synchronization;

/// <summary>
/// A bounded async producer-consumer queue backed by a <see cref="System.Threading.Channels.Channel{T}"/>.
/// Producers call <see cref="EnqueueAsync"/> or <see cref="TryEnqueue"/>; consumers iterate
/// <see cref="ConsumeAllAsync"/> which yields items as they arrive.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
public interface IWorkQueue<T>
{
    /// <summary>Number of items currently in the queue.</summary>
    int Count { get; }

    /// <summary><c>true</c> after <see cref="Complete"/> has been called and all items have been consumed.</summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Asynchronously writes <paramref name="item"/> to the queue.
    /// When the queue is full and configured with <c>BoundedChannelFullMode.Wait</c>, waits until space is available.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to write <paramref name="item"/> to the queue without waiting.
    /// Returns a failure <see cref="Result{T}"/> with <c>ConcurrencyErrors.QueueFull</c> if the queue
    /// is full, or <c>ConcurrencyErrors.QueueCompleted</c> if the queue has been completed.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing <paramref name="item"/> on success,
    /// or a failure with <c>ConcurrencyErrors.QueueFull</c> or <c>ConcurrencyErrors.QueueCompleted</c>.
    /// </returns>
    Result<T> TryEnqueue(T item);

    /// <summary>
    /// Signals that no more items will be produced. Consumers will drain remaining items
    /// then the <see cref="ConsumeAllAsync"/> enumerable will complete.
    /// </summary>
    void Complete();

    /// <summary>
    /// Returns an async sequence that yields all items as they are enqueued, completing when
    /// <see cref="Complete"/> is called and the queue is drained.
    /// </summary>
    /// <param name="cancellationToken">Token to stop consuming early.</param>
    /// <returns>An async sequence of items in the order they were enqueued.</returns>
    IAsyncEnumerable<T> ConsumeAllAsync(CancellationToken cancellationToken = default);
}
