using System.Collections.Concurrent;
using System.Threading.Channels;
using FluentAssertions;
using StangaNetLib.Concurrency.Synchronization;
using Xunit;

namespace StangaNetLib.Concurrency.Tests;

public sealed class WorkQueueStressTests
{
    [Fact]
    public async Task EnqueueAsync_WhenFullWithWaitMode_CancelledWait_ThrowsOperationCanceledException()
    {
        var queue = new BoundedWorkQueue<int>(2, BoundedChannelFullMode.Wait);
        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await queue.EnqueueAsync(3, cts.Token));
    }

    [Fact]
    public async Task ConcurrentProducers_AllItemsConsumedExactlyOnce()
    {
        const int producerCount = 5;
        const int itemsPerProducer = 20;
        var queue = new BoundedWorkQueue<int>(producerCount * itemsPerProducer);

        var producers = Enumerable.Range(0, producerCount).Select(p =>
            Task.Run(async () =>
            {
                for (var i = 0; i < itemsPerProducer; i++)
                    await queue.EnqueueAsync(p * itemsPerProducer + i);
            }));

        await Task.WhenAll(producers);
        queue.Complete();

        var received = new List<int>();
        await foreach (var item in queue.ConsumeAllAsync())
            received.Add(item);

        received.Should().HaveCount(producerCount * itemsPerProducer);
        received.Distinct().Should().HaveCount(producerCount * itemsPerProducer);
    }

    [Fact]
    public async Task MultipleConsumers_ConcurrentDrain_NoDuplicates()
    {
        const int itemCount = 60;
        var queue = new BoundedWorkQueue<int>(itemCount);
        for (var i = 0; i < itemCount; i++)
            await queue.EnqueueAsync(i);
        queue.Complete();

        var bag = new ConcurrentBag<int>();
        var c1 = Task.Run(async () => { await foreach (var item in queue.ConsumeAllAsync()) bag.Add(item); });
        var c2 = Task.Run(async () => { await foreach (var item in queue.ConsumeAllAsync()) bag.Add(item); });
        var c3 = Task.Run(async () => { await foreach (var item in queue.ConsumeAllAsync()) bag.Add(item); });

        await Task.WhenAll(c1, c2, c3);

        bag.Should().HaveCount(itemCount);
        bag.Distinct().Should().HaveCount(itemCount);
    }

    [Fact]
    public async Task DisposeAsync_CompletesChannel_ConsumeAllDrainsRemainingItems()
    {
        var queue = new BoundedWorkQueue<int>(10);
        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        await queue.DisposeAsync();

        var received = new List<int>();
        await foreach (var item in queue.ConsumeAllAsync())
            received.Add(item);

        received.Should().HaveCount(2);
        received.Should().Equal(1, 2);
    }

    [Fact]
    public async Task TryEnqueue_WhenFull_DropWriteMode_SucceedsButItemIsDropped()
    {
        // DropWrite: TryWrite returns true (write "accepted") but the overflow item is silently discarded.
        var queue = new BoundedWorkQueue<int>(2, BoundedChannelFullMode.DropWrite);
        await queue.EnqueueAsync(1);
        await queue.EnqueueAsync(2);

        var result = queue.TryEnqueue(3);

        result.IsSuccess.Should().BeTrue();
        queue.Count.Should().Be(2);
    }

    [Fact]
    public async Task EnqueueAsync_AfterComplete_ThrowsChannelClosedException()
    {
        var queue = new BoundedWorkQueue<int>(10);
        queue.Complete();
        await Task.Delay(10);

        var act = async () => await queue.EnqueueAsync(1);

        await act.Should().ThrowAsync<ChannelClosedException>();
    }

    [Fact]
    public async Task ConcurrentEnqueueAndConsume_HighThroughput_AllItemsDelivered()
    {
        const int itemCount = 200;
        var queue = new BoundedWorkQueue<int>(50);
        var received = new ConcurrentBag<int>();

        var consumer = Task.Run(async () =>
        {
            await foreach (var item in queue.ConsumeAllAsync())
                received.Add(item);
        });

        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < itemCount; i++)
                await queue.EnqueueAsync(i);
            queue.Complete();
        });

        await Task.WhenAll(producer, consumer);

        received.Should().HaveCount(itemCount);
        received.Distinct().Should().HaveCount(itemCount);
    }
}
