using StangaNetLib.Core.Common;

namespace StangaNetLib.Concurrency.Throttling;

/// <summary>
/// Limits the number of operations that can execute concurrently using a semaphore slot.
/// Callers <c>await WaitAsync()</c> to enter, then dispose the returned handle to release the slot.
/// </summary>
public interface IAsyncThrottle
{
    /// <summary>Maximum number of concurrent operations allowed.</summary>
    int MaxConcurrency { get; }

    /// <summary>Number of available slots at this instant.</summary>
    int AvailableSlots { get; }

    /// <summary>
    /// Waits until a concurrency slot is available, then returns a handle whose disposal
    /// releases the slot back to the pool.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that releases the slot when disposed.</returns>
    Task<IAsyncDisposable> WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a concurrency slot within <paramref name="timeout"/>.
    /// Returns a failure <see cref="Result{T}"/> with <c>ConcurrencyErrors.ThrottleTimeout</c>
    /// when the timeout elapses without a slot becoming available.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for a slot.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping an <see cref="IAsyncDisposable"/> slot handle,
    /// or a failure with <c>ConcurrencyErrors.ThrottleTimeout</c> if the timeout elapsed.
    /// </returns>
    Task<Result<IAsyncDisposable>> TryWaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
