using StangaNetLib.Core.Common;

namespace StangaNetLib.Concurrency.Core;

/// <summary>Well-known <see cref="Error"/> instances for StangaNetLib.Concurrency.</summary>
public static class ConcurrencyErrors
{
    /// <summary>Returned when a keyed lock cannot be acquired within the requested timeout.</summary>
    public static readonly Error LockTimeout = new("Concurrency.LockTimeout", "Could not acquire the lock within the specified timeout.");

    /// <summary>Returned when a work queue is full and the item cannot be enqueued.</summary>
    public static readonly Error QueueFull = new("Concurrency.QueueFull", "The work queue is full and cannot accept new items.");

    /// <summary>Returned when a work queue has been marked as complete and no more items can be enqueued.</summary>
    public static readonly Error QueueCompleted = new("Concurrency.QueueCompleted", "The work queue has been completed and does not accept new items.");

    /// <summary>Returned when a throttle slot cannot be acquired within the requested timeout.</summary>
    public static readonly Error ThrottleTimeout = new("Concurrency.ThrottleTimeout", "Could not acquire a throttle slot within the specified timeout.");
}
