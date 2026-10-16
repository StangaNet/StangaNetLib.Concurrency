using System.Threading.Channels;
using StangaNetLib.Concurrency.Throttling;
using StangaNetLib.Concurrency.Synchronization;

namespace StangaNetLib.Concurrency.Configuration;

/// <summary>Configuration for StangaNetLib.Concurrency services.</summary>
public sealed class ConcurrencySettings
{
    /// <summary>The <c>appsettings.json</c> section key used by <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> binding. Value: <c>"ConcurrencySettings"</c>.</summary>
    public const string SectionName = "ConcurrencySettings";

    /// <summary>Maximum concurrent slots for the default <see cref="IAsyncThrottle"/> registered in DI.
    /// Defaults to 10.
    /// </summary>
    public int DefaultThrottleMaxConcurrency { get; set; } = 10;

    /// <summary>Maximum capacity of the default <see cref="IWorkQueue{T}"/> registered in DI.
    /// Defaults to 1000.
    /// </summary>
    public int WorkQueueCapacity { get; set; } = 1_000;

    /// <summary>
    /// Behaviour when the work queue is full and a producer calls
    /// <see cref="IWorkQueue{T}.EnqueueAsync"/>.
    /// Defaults to <see cref="BoundedChannelFullMode.Wait"/>.
    /// </summary>
    public BoundedChannelFullMode WorkQueueFullMode { get; set; } = BoundedChannelFullMode.Wait;
}