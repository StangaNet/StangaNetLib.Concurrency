using System.Threading.Channels;

namespace StangaNetLib.Concurrency.Synchronization;

internal sealed class WorkQueueFactory : IWorkQueueFactory
{
    public IWorkQueue<T> Create<T>(int capacity, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
        => new BoundedWorkQueue<T>(capacity, fullMode);
}
