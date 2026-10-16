using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.Concurrency.Extensions;
using StangaNetLib.Concurrency.Locking;
using StangaNetLib.Concurrency.Scheduling;
using StangaNetLib.Concurrency.Synchronization;
using StangaNetLib.Concurrency.Throttling;

namespace StangaNetLib.Concurrency.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddStangaNetLibConcurrency_WithoutConfiguration_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddStangaNetLibConcurrency();

        await using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IKeyedLock>().Should().NotBeNull();
        provider.GetRequiredService<IKeyedLockFactory>().Should().NotBeNull();
        provider.GetRequiredService<IAsyncThrottle>().Should().NotBeNull();
        provider.GetRequiredService<IAsyncThrottleFactory>().Should().NotBeNull();
        provider.GetRequiredService<IWorkQueue<string>>().Should().NotBeNull();
        provider.GetRequiredService<IWorkQueueFactory>().Should().NotBeNull();
        provider.GetRequiredService<IDebouncer>().Should().NotBeNull();
        provider.GetRequiredService<IAtomicCounterFactory>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddStangaNetLibConcurrency_WithConfiguration_AppliesThrottleSetting()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConcurrencySettings:DefaultThrottleMaxConcurrency"] = "3",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddStangaNetLibConcurrency(configuration);

        await using var provider = services.BuildServiceProvider();
        var throttle = provider.GetRequiredService<IAsyncThrottle>();

        throttle.MaxConcurrency.Should().Be(3);
    }

    [Fact]
    public void AddStangaNetLibConcurrency_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddStangaNetLibConcurrency();
        services.AddStangaNetLibConcurrency();

        services.Count(d => d.ServiceType == typeof(IKeyedLock)).Should().Be(1);
        services.Count(d => d.ServiceType == typeof(IAsyncThrottleFactory)).Should().Be(1);
    }
}
