using StangaNetLib.Core.Common;
using StangaNetLib.Concurrency.Core;

namespace StangaNetLib.Concurrency.Throttling;

internal sealed class AsyncThrottle : IAsyncThrottle, IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    internal AsyncThrottle(int maxConcurrency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        MaxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public int MaxConcurrency { get; }

    public int AvailableSlots => _semaphore.CurrentCount;

    public async Task<IAsyncDisposable> WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ThrottleHandle(_semaphore);
    }

    public async Task<Result<IAsyncDisposable>> TryWaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var acquired = await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        if (!acquired)
            return Result<IAsyncDisposable>.Failure(ConcurrencyErrors.ThrottleTimeout);
        return Result<IAsyncDisposable>.Success(new ThrottleHandle(_semaphore));
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class ThrottleHandle : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _disposed;

        internal ThrottleHandle(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
