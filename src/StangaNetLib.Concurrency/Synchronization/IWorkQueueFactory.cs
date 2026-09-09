using System.Threading.Channels;

namespace StangaNetLib.Concurrency.Synchronization;

/// <summary>
/// Creates independent <see cref="IWorkQueue{T}"/> instances with custom capacity and overflow policies.
/// Use when multiple consumers need isolated queues with different configurations.
/// </summary>
public interface IWorkQueueFactory
{
    /// <summary>
    /// Creates a new bounded <see cref="IWorkQueue{T}"/> with the specified capacity and overflow policy.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="capacity">Maximum number of items the queue can hold. Must be ≥ 1.</param>
    /// <param name="fullMode">
    /// Behaviour when the queue is full and a producer calls <see cref="IWorkQueue{T}.EnqueueAsync"/>.
    /// Defaults to <see cref="BoundedChannelFullMode.Wait"/>.
    /// </param>
    /// <returns>A new independent <see cref="IWorkQueue{T}"/> with the specified capacity and policy.</returns>
    IWorkQueue<T> Create<T>(int capacity, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait);
}
