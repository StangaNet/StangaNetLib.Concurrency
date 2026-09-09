using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StangaNetLib.Concurrency.Configuration;
using StangaNetLib.Concurrency.Locking;
using StangaNetLib.Concurrency.Scheduling;
using StangaNetLib.Concurrency.Synchronization;
using StangaNetLib.Concurrency.Throttling;

namespace StangaNetLib.Concurrency.Extensions;

/// <summary>
/// Extension methods for registering StangaNetLib.Concurrency services.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// {
///   "ConcurrencySettings": {
///     "DefaultThrottleMaxConcurrency": 10,
///     "WorkQueueCapacity": 1000,
///     "WorkQueueFullMode": "Wait"
///   }
/// }
/// </code>
/// Program.cs:
/// <code>
/// builder.Services.AddStangaNetLibConcurrency(builder.Configuration);
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all StangaNetLib.Concurrency services using configuration from
    /// the <c>ConcurrencySettings</c> section of <paramref name="configuration"/>.
    /// Registers: <see cref="Locking.IKeyedLock"/>, <see cref="Locking.IKeyedLockFactory"/>,
    /// <see cref="Throttling.IAsyncThrottle"/>, <see cref="Throttling.IAsyncThrottleFactory"/>,
    /// <see cref="Synchronization.IWorkQueue{T}"/>, <see cref="Synchronization.IWorkQueueFactory"/>,
    /// <see cref="Scheduling.IDebouncer"/>, <see cref="Synchronization.IAtomicCounterFactory"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root used to bind <c>ConcurrencySettings</c>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibConcurrency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConcurrencySettings>(configuration.GetSection(ConcurrencySettings.SectionName));
        return RegisterCore(services);
    }

    /// <summary>
    /// Registers all StangaNetLib.Concurrency services using default settings.
    /// Registers: <see cref="Locking.IKeyedLock"/>, <see cref="Locking.IKeyedLockFactory"/>,
    /// <see cref="Throttling.IAsyncThrottle"/>, <see cref="Throttling.IAsyncThrottleFactory"/>,
    /// <see cref="Synchronization.IWorkQueue{T}"/>, <see cref="Synchronization.IWorkQueueFactory"/>,
    /// <see cref="Scheduling.IDebouncer"/>, <see cref="Synchronization.IAtomicCounterFactory"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibConcurrency(this IServiceCollection services)
    {
        services.Configure<ConcurrencySettings>(_ => { });
        return RegisterCore(services);
    }

    private static IServiceCollection RegisterCore(IServiceCollection services)
    {
        services.TryAddSingleton<IKeyedLock, KeyedLock>();
        services.TryAddSingleton<IKeyedLockFactory, KeyedLockFactory>();
        services.TryAddSingleton<IAsyncThrottleFactory, AsyncThrottleFactory>();
        services.TryAddSingleton<IAsyncThrottle>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<ConcurrencySettings>>().Value;
            return new AsyncThrottle(settings.DefaultThrottleMaxConcurrency);
        });
        services.TryAddSingleton(typeof(IWorkQueue<>), typeof(BoundedWorkQueue<>));
        services.TryAddSingleton<IWorkQueueFactory, WorkQueueFactory>();
        services.TryAddSingleton<IDebouncer, Debouncer>();
        services.TryAddSingleton<IAtomicCounterFactory, AtomicCounterFactory>();
        return services;
    }
}
