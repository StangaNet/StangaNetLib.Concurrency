using FluentAssertions;
using StangaNetLib.Concurrency.Synchronization;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class AtomicCounterTests
{
    [Fact]
    public void InitialValue_DefaultsToZero()
    {
        var counter = new AtomicCounter();
        counter.Value.Should().Be(0);
    }

    [Fact]
    public void InitialValue_CanBeSet()
    {
        var counter = new AtomicCounter(42);
        counter.Value.Should().Be(42);
    }

    [Fact]
    public void Increment_ReturnsNewValue()
    {
        var counter = new AtomicCounter(0);
        counter.Increment().Should().Be(1);
        counter.Value.Should().Be(1);
    }

    [Fact]
    public void Decrement_ReturnsNewValue()
    {
        var counter = new AtomicCounter(5);
        counter.Decrement().Should().Be(4);
        counter.Value.Should().Be(4);
    }

    [Fact]
    public void Add_NegativeDelta_DecreasesValue()
    {
        var counter = new AtomicCounter(10);
        counter.Add(-3).Should().Be(7);
    }

    [Fact]
    public void Reset_SetsValueAndReturnsPrevious()
    {
        var counter = new AtomicCounter(99);
        var prev = counter.Reset(0);
        prev.Should().Be(99);
        counter.Value.Should().Be(0);
    }

    [Fact]
    public void TryUpdate_MatchingExpected_ReturnsTrue()
    {
        var counter = new AtomicCounter(10);
        counter.TryUpdate(10, 20).Should().BeTrue();
        counter.Value.Should().Be(20);
    }

    [Fact]
    public void TryUpdate_NonMatchingExpected_ReturnsFalse()
    {
        var counter = new AtomicCounter(10);
        counter.TryUpdate(99, 20).Should().BeFalse();
        counter.Value.Should().Be(10);
    }

    [Fact]
    public async Task Increment_FromMultipleThreads_IsThreadSafe()
    {
        var counter = new AtomicCounter(0);
        const int iterations = 1_000;
        const int threadCount = 10;

        await Task.WhenAll(Enumerable.Range(0, threadCount).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < iterations; i++)
                    counter.Increment();
            })));

        counter.Value.Should().Be(threadCount * iterations);
    }

    [Fact]
    public void Factory_Create_ReturnsIndependentCounters()
    {
        var factory = new AtomicCounterFactory();
        var c1 = factory.Create(5);
        var c2 = factory.Create(10);

        c1.Value.Should().Be(5);
        c2.Value.Should().Be(10);
        c1.Should().NotBeSameAs(c2);
    }

    [Fact]
    public void Factory_GetOrCreate_ReturnsSameInstanceForSameName()
    {
        var factory = new AtomicCounterFactory();
        var c1 = factory.GetOrCreate("shared", 100);
        var c2 = factory.GetOrCreate("shared", 999);

        c1.Should().BeSameAs(c2);
        c1.Value.Should().Be(100);
    }
}
