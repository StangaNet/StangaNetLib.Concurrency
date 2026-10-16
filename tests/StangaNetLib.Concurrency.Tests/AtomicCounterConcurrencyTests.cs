using FluentAssertions;
using StangaNetLib.Concurrency.Synchronization;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class AtomicCounterConcurrencyTests
{
    [Fact]
    public async Task ConcurrentIncrementAndDecrement_EqualOps_NetZero()
    {
        var counter = new AtomicCounter(0);
        const int ops = 5_000;

        var inc = Task.Run(() => { for (var i = 0; i < ops; i++) counter.Increment(); });
        var dec = Task.Run(() => { for (var i = 0; i < ops; i++) counter.Decrement(); });

        await Task.WhenAll(inc, dec);

        counter.Value.Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentTryUpdate_RaceCondition_ExactlyOneSucceeds()
    {
        var counter = new AtomicCounter(0);
        var successCount = 0;
        const int taskCount = 20;

        var tasks = Enumerable.Range(0, taskCount).Select(_ =>
            Task.Run(() =>
            {
                if (counter.TryUpdate(0, 1))
                    Interlocked.Increment(ref successCount);
            }));

        await Task.WhenAll(tasks);

        successCount.Should().Be(1);
        counter.Value.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentAdd_MixedDeltas_ResultEqualsNetSum()
    {
        var counter = new AtomicCounter(0);
        const int ops = 1_000;

        var addTask = Task.Run(() => { for (var i = 0; i < ops; i++) counter.Add(3); });
        var subTask = Task.Run(() => { for (var i = 0; i < ops; i++) counter.Add(-1); });

        await Task.WhenAll(addTask, subTask);

        counter.Value.Should().Be(ops * 3 + ops * -1);
    }

    [Fact]
    public async Task ParallelAdd_LargePositiveDelta_ProducesCorrectTotal()
    {
        var counter = new AtomicCounter(0);
        const int threads = 10;
        const int addsPerThread = 1_000;
        const long delta = 7;

        await Task.WhenAll(Enumerable.Range(0, threads).Select(_ =>
            Task.Run(() => { for (var i = 0; i < addsPerThread; i++) counter.Add(delta); })));

        counter.Value.Should().Be(threads * addsPerThread * delta);
    }

    [Fact]
    public void Decrement_BelowZero_ProducesNegativeValue()
    {
        var counter = new AtomicCounter(0);

        counter.Decrement();
        counter.Decrement();
        counter.Decrement();

        counter.Value.Should().Be(-3);
    }

    [Fact]
    public void LongMaxValue_Increment_WrapsToLongMinValue()
    {
        var counter = new AtomicCounter(long.MaxValue);

        var result = counter.Increment();

        result.Should().Be(long.MinValue);
    }

    [Fact]
    public async Task ConcurrentCounters_FromFactory_AreIndependent()
    {
        var factory = new AtomicCounterFactory();
        var c1 = factory.Create(0);
        var c2 = factory.Create(0);

        await Task.WhenAll(
            Task.Run(() => { for (var i = 0; i < 1_000; i++) c1.Increment(); }),
            Task.Run(() => { for (var i = 0; i < 2_000; i++) c2.Increment(); }));

        c1.Value.Should().Be(1_000);
        c2.Value.Should().Be(2_000);
    }
}
