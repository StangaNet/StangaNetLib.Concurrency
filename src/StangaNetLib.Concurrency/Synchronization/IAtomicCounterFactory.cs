namespace StangaNetLib.Concurrency.Synchronization;

/// <summary>
/// Creates and manages named <see cref="IAtomicCounter"/> instances.
/// Use <see cref="GetOrCreate"/> to share a counter across components by name,
/// or <see cref="Create"/> for a private counter that is not tracked.
/// </summary>
public interface IAtomicCounterFactory
{
    /// <summary>
    /// Creates a new untracked <see cref="IAtomicCounter"/> starting at <paramref name="initialValue"/>.
    /// The returned instance is not registered by name; each call produces a distinct counter.
    /// </summary>
    /// <param name="initialValue">Starting value. Defaults to 0.</param>
    /// <returns>A new, unregistered <see cref="IAtomicCounter"/> starting at <paramref name="initialValue"/>.</returns>
    IAtomicCounter Create(long initialValue = 0);

    /// <summary>
    /// Returns the existing counter registered under <paramref name="name"/>, or creates and registers
    /// a new one starting at <paramref name="initialValue"/> if none exists yet.
    /// </summary>
    /// <param name="name">Unique name for the counter.</param>
    /// <param name="initialValue">Starting value used only when the counter is first created.</param>
    /// <returns>The named counter, newly created or previously registered.</returns>
    IAtomicCounter GetOrCreate(string name, long initialValue = 0);
}
