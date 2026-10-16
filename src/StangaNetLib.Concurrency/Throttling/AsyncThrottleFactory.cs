namespace StangaNetLib.Concurrency.Throttling;

internal sealed class AsyncThrottleFactory : IAsyncThrottleFactory
{
    public IAsyncThrottle Create(int maxConcurrency) => new AsyncThrottle(maxConcurrency);
}
