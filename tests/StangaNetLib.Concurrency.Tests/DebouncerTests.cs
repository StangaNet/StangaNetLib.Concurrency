using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StangaNetLib.Concurrency.Scheduling;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class DebouncerTests : IDisposable
{
    private readonly Debouncer _sut = new(NullLogger<Debouncer>.Instance);

    [Fact]
    public async Task Debounce_SingleTrigger_ExecutesAfterDelay()
    {
        var executed = false;
        _sut.Debounce("key1", _ => { executed = true; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));

        await Task.Delay(150);

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Debounce_RapidTriggers_ExecutesOnlyOnce()
    {
        var count = 0;
        for (var i = 0; i < 5; i++)
        {
            _sut.Debounce("key2", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(80));
            await Task.Delay(20);
        }

        await Task.Delay(300);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Debounce_DifferentKeys_ExecuteIndependently()
    {
        var count = 0;
        _sut.Debounce("keyA", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));
        _sut.Debounce("keyB", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));

        await Task.Delay(200);

        count.Should().Be(2);
    }

    [Fact]
    public async Task Debounce_AfterDispose_DoesNotExecute()
    {
        var executed = false;
        _sut.Debounce("key3", _ => { executed = true; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        _sut.Dispose();

        await Task.Delay(200);

        executed.Should().BeFalse();
    }

    [Fact]
    public void Debounce_AfterDispose_Throws()
    {
        _sut.Dispose();
        var act = () => _sut.Debounce("key4", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(50));
        act.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose() => _sut.Dispose();
}
