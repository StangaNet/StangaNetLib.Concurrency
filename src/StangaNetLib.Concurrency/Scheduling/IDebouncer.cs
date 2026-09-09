namespace StangaNetLib.Concurrency.Scheduling;

/// <summary>
/// Debounces high-frequency triggers so that the underlying action executes only after the
/// signal has been quiet for the configured delay. Each call resets the timer for that key.
/// Useful for coalescing rapid events (e.g., search-as-you-type, file-watcher callbacks).
/// </summary>
public interface IDebouncer : IDisposable
{
    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="delay"/>.
    /// If called again with the same <paramref name="key"/> before the delay elapses,
    /// the previous scheduled execution is cancelled and a new countdown starts.
    /// </summary>
    /// <param name="key">Identifier for this debounce slot. Independent keys do not interfere.</param>
    /// <param name="action">The async operation to execute after the quiet period.</param>
    /// <param name="delay">How long to wait after the last trigger before executing.</param>
    void Debounce(string key, Func<CancellationToken, Task> action, TimeSpan delay);

    /// <summary>
    /// Cancels any pending debounced execution for <paramref name="key"/> without disposing the debouncer.
    /// Has no effect if no execution is pending for that key.
    /// </summary>
    /// <param name="key">The debounce slot to cancel.</param>
    /// <returns><c>true</c> if a pending execution was found and cancelled; <c>false</c> if the slot was already idle.</returns>
    bool Cancel(string key);
}
