using System.Collections.Concurrent;
using FluentAssertions;
using StangaNetLib.Concurrency.Core;
using StangaNetLib.Concurrency.Locking;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class KeyedLockTests
{
    private readonly KeyedLock _sut = new();

    [Fact]
    public async Task AcquireAsync_SameKey_SerializesAccess()
    {
        var results = new ConcurrentQueue<int>();
        var t1Holding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseT1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var t1 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("resource-1");
            results.Enqueue(1);
            t1Holding.SetResult();
            await releaseT1.Task;
            results.Enqueue(2);
        });

        await t1Holding.Task;

        var t2 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("resource-1");
            results.Enqueue(3);
        });

        // Ensure t2 is blocked on the same key before t1 releases.
        await Task.Delay(50);
        releaseT1.SetResult();

        await Task.WhenAll(t1, t2);

        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_RunConcurrently()
    {
        var started = new ConcurrentQueue<int>();
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = 0;

        var t1 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("key-A");
            started.Enqueue(1);
            if (Interlocked.Increment(ref seen) == 2)
                bothStarted.TrySetResult();
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        });
        var t2 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("key-B");
            started.Enqueue(2);
            if (Interlocked.Increment(ref seen) == 2)
                bothStarted.TrySetResult();
            await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        });

        await Task.WhenAll(t1, t2);

        // If keys were serialized, one task would time out waiting for the other to start.
        started.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLocked_ReturnsFailure()
    {
        await using var firstHandle = await _sut.AcquireAsync("locked-key");

        var result = await _sut.TryAcquireAsync("locked-key", TimeSpan.FromMilliseconds(50));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConcurrencyErrors.LockTimeout);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenNotLocked_ReturnsSuccess()
    {
        var result = await _sut.TryAcquireAsync("free-key", TimeSpan.FromMilliseconds(100));

        result.IsSuccess.Should().BeTrue();
        await result.Value!.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_HandleDisposedTwice_DoesNotThrow()
    {
        var handle = await _sut.AcquireAsync("idempotent-key");

        await handle.DisposeAsync();
        var act = async () => await handle.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AcquireAsync_AfterRelease_CanAcquireAgain()
    {
        await using (await _sut.AcquireAsync("reuse-key")) { }

        var result = await _sut.TryAcquireAsync("reuse-key", TimeSpan.FromMilliseconds(50));

        result.IsSuccess.Should().BeTrue();
        await result.Value!.DisposeAsync();
    }
}
