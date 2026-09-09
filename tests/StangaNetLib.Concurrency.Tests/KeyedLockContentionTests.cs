using FluentAssertions;
using StangaNetLib.Concurrency.Core;
using StangaNetLib.Concurrency.Locking;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class KeyedLockContentionTests
{
    private readonly KeyedLock _sut = new();

    [Fact]
    public async Task AcquireAsync_SameKey_OnlyOneTaskHoldsLockAtATime()
    {
        var concurrent = 0;
        var maxObserved = 0;

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var handle = await _sut.AcquireAsync("hot-key");
            var current = Interlocked.Increment(ref concurrent);
            int prev;
            do { prev = maxObserved; }
            while (current > prev && Interlocked.CompareExchange(ref maxObserved, current, prev) != prev);
            await Task.Delay(5);
            Interlocked.Decrement(ref concurrent);
        });

        await Task.WhenAll(tasks);

        maxObserved.Should().Be(1);
    }

    [Fact]
    public async Task AcquireAsync_HighContention_TwentyTasks_AllComplete()
    {
        var completed = 0;

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var handle = await _sut.AcquireAsync("contended-key");
            Interlocked.Increment(ref completed);
        });

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));

        completed.Should().Be(20);
    }

    [Fact]
    public async Task AcquireAsync_CancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        await using var held = await _sut.AcquireAsync("cancel-key");
        using var cts = new CancellationTokenSource();

        var waiter = Task.Run(async () => await _sut.AcquireAsync("cancel-key", cts.Token));
        await Task.Delay(20);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
    }

    [Fact]
    public async Task AcquireAsync_AfterCancellation_SameKeyAcquirableAgain()
    {
        var held = await _sut.AcquireAsync("recover-key");
        using var cts = new CancellationTokenSource();

        var waiter = Task.Run(async () => await _sut.AcquireAsync("recover-key", cts.Token));
        await Task.Delay(20);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        await held.DisposeAsync();
        var result = await _sut.TryAcquireAsync("recover-key", TimeSpan.FromMilliseconds(100));

        result.IsSuccess.Should().BeTrue();
        await result.Value!.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_AllAcquiredConcurrently()
    {
        const int keyCount = 8;
        var acquiredCount = 0;
        var allAcquired = new TaskCompletionSource();
        var releaseSignal = new TaskCompletionSource();

        var tasks = Enumerable.Range(0, keyCount).Select(async i =>
        {
            await using var handle = await _sut.AcquireAsync($"parallel-key-{i}");
            if (Interlocked.Increment(ref acquiredCount) == keyCount)
                allAcquired.TrySetResult();
            await releaseSignal.Task;
        }).ToList();

        var didAllAcquire = await Task.WhenAny(allAcquired.Task, Task.Delay(2000)) == allAcquired.Task;
        releaseSignal.SetResult();
        await Task.WhenAll(tasks);

        didAllAcquire.Should().BeTrue("different-key locks must not block each other");
    }

    [Fact]
    public async Task TryAcquireAsync_ZeroTimeout_WhenLocked_ReturnsFailureImmediately()
    {
        await using var held = await _sut.AcquireAsync("zero-timeout-key");

        var result = await _sut.TryAcquireAsync("zero-timeout-key", TimeSpan.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConcurrencyErrors.LockTimeout);
    }

    [Fact]
    public async Task TryAcquireAsync_AfterFailedAttempt_SubsequentAcquireSucceeds()
    {
        var held = await _sut.AcquireAsync("retry-key");
        var fail = await _sut.TryAcquireAsync("retry-key", TimeSpan.FromMilliseconds(10));
        fail.IsFailure.Should().BeTrue();

        await held.DisposeAsync();

        var success = await _sut.TryAcquireAsync("retry-key", TimeSpan.FromMilliseconds(200));
        success.IsSuccess.Should().BeTrue();
        await success.Value!.DisposeAsync();
    }
}
