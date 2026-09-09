namespace StangaNetLib.Concurrency.Synchronization;

/// <summary>
/// A thread-safe 64-bit integer counter backed by <see cref="System.Threading.Interlocked"/> operations.
/// All methods are lock-free and safe to call from multiple threads simultaneously.
/// </summary>
public interface IAtomicCounter
{
    /// <summary>Reads the current value atomically.</summary>
    long Value { get; }

    /// <summary>Atomically increments the counter by 1 and returns the new value.</summary>
    /// <returns>The counter value after the increment.</returns>
    long Increment();

    /// <summary>Atomically decrements the counter by 1 and returns the new value.</summary>
    /// <returns>The counter value after the decrement.</returns>
    long Decrement();

    /// <summary>Atomically adds <paramref name="delta"/> to the counter and returns the new value.</summary>
    /// <param name="delta">The amount to add. May be negative to perform a subtraction.</param>
    /// <returns>The counter value after the addition.</returns>
    long Add(long delta);

    /// <summary>Atomically sets the counter to <paramref name="value"/> and returns the previous value.</summary>
    /// <param name="value">The value to assign. Defaults to 0.</param>
    /// <returns>The counter value that was replaced.</returns>
    long Reset(long value = 0);

    /// <summary>
    /// Atomically sets the counter to <paramref name="newValue"/> only if the current value equals
    /// <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="expectedValue">The value the counter must currently hold for the update to proceed.</param>
    /// <param name="newValue">The value to write if the comparison succeeds.</param>
    /// <returns><c>true</c> if the counter was updated; <c>false</c> if the current value differed from <paramref name="expectedValue"/>.</returns>
    bool TryUpdate(long expectedValue, long newValue);
}
