using FluentAssertions;
using StangaNetLib.Concurrency.Throttling;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class AsyncThrottleTests
{
    [Fact]
    public void Constructor_InvalidConcurrency_Throws()
    {
        var act = () => new AsyncThrottle(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WaitAsync_RespectsMaxConcurrency()
    {
        var throttle = new AsyncThrottle(2);
        var concurrent = 0;
        var maxObserved = 0;
        var tasks = Enumerable.Range(0, 6).Select(async _ =>
        {
            await using var handle = await throttle.WaitAsync();
            var current = Interlocked.Increment(ref concurrent);
            Interlocked.Exchange(ref maxObserved, Math.Max(maxObserved, current));
            await Task.Delay(30);
            Interlocked.Decrement(ref concurrent);
        });

        await Task.WhenAll(tasks);

        maxObserved.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task WaitAsync_SlotReleasedOnDispose_AllowsNextCaller()
    {
        var throttle = new AsyncThrottle(1);
        var handle = await throttle.WaitAsync();

        throttle.AvailableSlots.Should().Be(0);
        await handle.DisposeAsync();
        throttle.AvailableSlots.Should().Be(1);
    }

    [Fact]
    public async Task WaitAsync_HandleDisposedTwice_DoesNotOverRelease()
    {
        var throttle = new AsyncThrottle(1);
        var handle = await throttle.WaitAsync();

        await handle.DisposeAsync();
        await handle.DisposeAsync();

        throttle.AvailableSlots.Should().Be(1);
    }

    [Fact]
    public void Factory_Create_ReturnsIndependentInstance()
    {
        var factory = new AsyncThrottleFactory();
        var t1 = factory.Create(3);
        var t2 = factory.Create(5);

        t1.MaxConcurrency.Should().Be(3);
        t2.MaxConcurrency.Should().Be(5);
        t1.Should().NotBeSameAs(t2);
    }
}
