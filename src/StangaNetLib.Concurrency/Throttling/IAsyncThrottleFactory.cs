namespace StangaNetLib.Concurrency.Throttling;

/// <summary>
/// Creates independent <see cref="IAsyncThrottle"/> instances with custom concurrency limits.
/// Use when you need multiple throttles with different caps within the same application.
/// </summary>
public interface IAsyncThrottleFactory
{
    /// <summary>
    /// Creates a new <see cref="IAsyncThrottle"/> that allows up to <paramref name="maxConcurrency"/>
    /// simultaneous operations.
    /// </summary>
    /// <param name="maxConcurrency">Maximum number of concurrent slots. Must be ≥ 1.</param>
    /// <returns>A new independent <see cref="IAsyncThrottle"/> with the specified concurrency limit.</returns>
    IAsyncThrottle Create(int maxConcurrency);
}
