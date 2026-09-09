using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace StangaNetLib.Concurrency.Scheduling;

/// <summary>
/// Debounce implementation that stores one <see cref="CancellationTokenSource"/> per key.
/// Each new call for a key cancels and replaces the previous CTS before scheduling a new delay,
/// so only the last trigger within a quiet window executes the action.
/// </summary>
internal sealed class Debouncer : IDebouncer
{
    private readonly ILogger<Debouncer> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private bool _disposed;

    internal Debouncer(ILogger<Debouncer> logger) => _logger = logger;

    public void Debounce(string key, Func<CancellationToken, Task> action, TimeSpan delay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(action);
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationTokenSource newCts;
        lock (_sync)
        {
            if (_disposed) return;
            if (_pending.TryRemove(key, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            newCts = new CancellationTokenSource();
            _pending[key] = newCts;
        }

        _ = RunAfterDelayAsync(key, action, delay, newCts);
    }

    public bool Cancel(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_pending.TryRemove(key, out var cts))
            return false;
        cts.Cancel();
        cts.Dispose();
        return true;
    }

    private async Task RunAfterDelayAsync(string key, Func<CancellationToken, Task> action, TimeSpan delay, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(delay, cts.Token).ConfigureAwait(false);
            // Only remove our own CTS entry — a newer Debounce call may have replaced it.
            _pending.TryRemove(new KeyValuePair<string, CancellationTokenSource>(key, cts));
            await action(CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debounced action for key '{Key}' threw an unhandled exception.", key);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
        }
        foreach (var cts in _pending.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _pending.Clear();
    }
}
