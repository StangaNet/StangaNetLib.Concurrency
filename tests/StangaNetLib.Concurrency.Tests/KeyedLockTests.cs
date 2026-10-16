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
        var results = new List<int>();
        var t1 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("resource-1");
            results.Add(1);
            await Task.Delay(50);
            results.Add(2);
        });
        await Task.Delay(10);
        var t2 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("resource-1");
            results.Add(3);
        });

        await Task.WhenAll(t1, t2);

        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_RunConcurrently()
    {
        var started = new List<int>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var t1 = Task.Run(async () =>
        {
            await using var handle = await _sut.AcquireAsync("key-A");
            started.Add(1);
            await Task.Delay(100);
        });
        var t2 = Task.Run(async () =>
        {
            await Task.Delay(20);
            await using var handle = await _sut.AcquireAsync("key-B");
            started.Add(2);
        });

        await Task.WhenAll(t1, t2).WaitAsync(cts.Token);

        started.Should().Contain(1).And.Contain(2);
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
