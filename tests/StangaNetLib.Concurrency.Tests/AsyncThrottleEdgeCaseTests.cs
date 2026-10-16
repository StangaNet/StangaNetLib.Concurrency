using FluentAssertions;
using StangaNetLib.Concurrency.Core;
using StangaNetLib.Concurrency.Throttling;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class AsyncThrottleEdgeCaseTests
{
    [Fact]
    public async Task WaitAsync_CapacityOne_EnforcesStrictSerialExecution()
    {
        var throttle = new AsyncThrottle(1);
        var concurrent = 0;
        var maxObserved = 0;

        var tasks = Enumerable.Range(0, 6).Select(async _ =>
        {
            await using var handle = await throttle.WaitAsync();
            var current = Interlocked.Increment(ref concurrent);
            int prev;
            do { prev = maxObserved; }
            while (current > prev && Interlocked.CompareExchange(ref maxObserved, current, prev) != prev);
            await Task.Delay(10);
            Interlocked.Decrement(ref concurrent);
        });

        await Task.WhenAll(tasks);

        maxObserved.Should().Be(1);
    }

    [Fact]
    public async Task TryWaitAsync_AllSlotsFull_ReturnsThrottleTimeout()
    {
        var throttle = new AsyncThrottle(2);
        await using var h1 = await throttle.WaitAsync();
        await using var h2 = await throttle.WaitAsync();

        var result = await throttle.TryWaitAsync(TimeSpan.FromMilliseconds(30));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConcurrencyErrors.ThrottleTimeout);
    }

    [Fact]
    public async Task WaitAsync_CancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        var throttle = new AsyncThrottle(1);
        await using var held = await throttle.WaitAsync();
        using var cts = new CancellationTokenSource();

        var waiter = Task.Run(async () => await throttle.WaitAsync(cts.Token));
        await Task.Delay(20);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
    }

    [Fact]
    public async Task WaitAsync_CancelledWithoutSlot_AvailableSlotsUnchanged()
    {
        var throttle = new AsyncThrottle(1);
        await using var held = await throttle.WaitAsync();
        using var cts = new CancellationTokenSource();

        var waiter = Task.Run(async () => await throttle.WaitAsync(cts.Token));
        await Task.Delay(20);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        throttle.AvailableSlots.Should().Be(0);
    }

    [Fact]
    public async Task AvailableSlots_UnderConcurrentLoad_NeverExceedsMax()
    {
        const int max = 4;
        var throttle = new AsyncThrottle(max);
        var maxUsed = 0;

        var tasks = Enumerable.Range(0, 12).Select(async _ =>
        {
            await using var handle = await throttle.WaitAsync();
            var used = max - throttle.AvailableSlots;
            int prev;
            do { prev = maxUsed; }
            while (used > prev && Interlocked.CompareExchange(ref maxUsed, used, prev) != prev);
            await Task.Delay(10);
        });

        await Task.WhenAll(tasks);

        maxUsed.Should().BeLessThanOrEqualTo(max);
    }

    [Fact]
    public async Task WaitAsync_FillAllSlots_ThenReleaseAll_RestoresFullCapacity()
    {
        const int max = 3;
        var throttle = new AsyncThrottle(max);

        var handles = new IAsyncDisposable[max];
        for (var i = 0; i < max; i++)
            handles[i] = await throttle.WaitAsync();

        throttle.AvailableSlots.Should().Be(0);

        for (var i = 0; i < max; i++)
            await handles[i].DisposeAsync();

        throttle.AvailableSlots.Should().Be(max);
    }

    [Fact]
    public async Task TryWaitAsync_SlotReleasedBeforeTimeout_Succeeds()
    {
        var throttle = new AsyncThrottle(1);
        var held = await throttle.WaitAsync();

        var tryTask = throttle.TryWaitAsync(TimeSpan.FromMilliseconds(500));
        await Task.Delay(50);
        await held.DisposeAsync();
        var result = await tryTask;

        result.IsSuccess.Should().BeTrue();
        await result.Value!.DisposeAsync();
    }
}
