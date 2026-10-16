using System.Threading.Channels;
using FluentAssertions;
using StangaNetLib.Concurrency.Core;
using StangaNetLib.Concurrency.Synchronization;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class WorkQueueTests
{
    [Fact]
    public async Task EnqueueAsync_ThenConsumeAll_ReturnsItemsInOrder()
    {
        var queue = new BoundedWorkQueue<int>(10);

        for (var i = 1; i <= 5; i++)
            await queue.EnqueueAsync(i);
        queue.Complete();

        var received = new List<int>();
        await foreach (var item in queue.ConsumeAllAsync())
            received.Add(item);

        received.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task TryEnqueue_WhenFull_ReturnsQueueFullError()
    {
        // BoundedChannelFullMode.Wait causes TryWrite to return false when at capacity
        var queue = new BoundedWorkQueue<int>(2, BoundedChannelFullMode.Wait);

        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        var result = queue.TryEnqueue(3);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConcurrencyErrors.QueueFull);
    }

    [Fact]
    public async Task TryEnqueue_WhenCompleted_ReturnsQueueCompletedError()
    {
        var queue = new BoundedWorkQueue<int>(10);
        queue.Complete();

        await Task.Delay(20);

        var result = queue.TryEnqueue(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConcurrencyErrors.QueueCompleted);
    }

    [Fact]
    public async Task ConsumeAllAsync_CancelToken_ThrowsWhileWaitingForItems()
    {
        // Queue is empty and not completed: consumer blocks waiting for items.
        // Cancellation should propagate while the consumer is suspended in WaitToReadAsync.
        var queue = new BoundedWorkQueue<int>(100);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in queue.ConsumeAllAsync(cts.Token))
            {
                _ = item; // should never execute
            }
        });
    }

    [Fact]
    public async Task Count_ReflectsEnqueuedItems()
    {
        var queue = new BoundedWorkQueue<string>(20);
        await queue.EnqueueAsync("a");
        await queue.EnqueueAsync("b");

        queue.Count.Should().Be(2);
    }
}
