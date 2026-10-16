using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StangaNetLib.Concurrency.Scheduling;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class DebouncerCancellationTests : IDisposable
{
    private readonly Debouncer _sut = new(NullLogger<Debouncer>.Instance);

    [Fact]
    public void Cancel_PendingKey_ReturnsTrue()
    {
        _sut.Debounce("cancel-me", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(500));

        var result = _sut.Cancel("cancel-me");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_PendingKey_ActionDoesNotExecute()
    {
        var executed = false;
        _sut.Debounce("no-fire", _ => { executed = true; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(80));

        _sut.Cancel("no-fire");
        await Task.Delay(250);

        executed.Should().BeFalse();
    }

    [Fact]
    public void Cancel_NonExistentKey_ReturnsFalse()
    {
        var result = _sut.Cancel("unknown-key");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_AfterActionExecuted_ReturnsFalse()
    {
        var executed = new TaskCompletionSource();
        _sut.Debounce("fired", _ => { executed.TrySetResult(); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(30));

        await executed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(20);

        var result = _sut.Cancel("fired");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_OneKey_OtherKeyStillFires()
    {
        var count = 0;
        _sut.Debounce("key-a", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));
        _sut.Debounce("key-b", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));

        _sut.Cancel("key-a");
        await Task.Delay(250);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Debounce_AfterCancel_SameKeyCanFireAgain()
    {
        var count = 0;
        _sut.Debounce("reuse", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));
        _sut.Cancel("reuse");

        _sut.Debounce("reuse", _ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(50));
        await Task.Delay(250);

        count.Should().Be(1);
    }

    public void Dispose() => _sut.Dispose();
}
