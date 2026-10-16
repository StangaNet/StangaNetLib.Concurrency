# StangaNetLib.Concurrency

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Concurrency/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Concurrency/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)

Simplified threading and concurrent data-access utilities for .NET: keyed locks, async throttling, bounded work queues, debouncing, and atomic counters. Built on [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core) (`Result` / `Error`).

## Design Philosophy

- **Domain-centric**: primitives grouped by purpose (`Locking`, `Throttling`, `Synchronization`, `Scheduling`).
- **Functional failures**: timeouts and full queues return `Result` / `ConcurrencyErrors`; exceptions stay for invalid arguments.
- **Defensive**: guard clauses and thread-safe primitives for high-contention scenarios.
- **Modern .NET**: `net8.0` / `net9.0`, `IAsyncDisposable`, `ValueTask`-friendly APIs.

## Installation

The package is hosted on **GitHub Packages** (pulls `StangaNetLib.Core` from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Concurrency" Version="1.0.0" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Concurrency.Locking` | `IKeyedLock`, `IKeyedLockFactory` | Per-resource async mutual exclusion |
| `StangaNetLib.Concurrency.Throttling` | `IAsyncThrottle`, `IAsyncThrottleFactory` | Semaphore-based concurrency limiter |
| `StangaNetLib.Concurrency.Synchronization` | `IWorkQueue<T>`, `IWorkQueueFactory`, `IAtomicCounter`, `IAtomicCounterFactory` | Bounded queues and lock-free counters |
| `StangaNetLib.Concurrency.Scheduling` | `IDebouncer` | Per-key debouncing of async actions |
| `StangaNetLib.Concurrency.Core` | `ConcurrencyErrors` | Typed `Error` constants |
| `StangaNetLib.Concurrency.Configuration` | `ConcurrencySettings` | Options POCO |
| `StangaNetLib.Concurrency.Extensions` | `AddStangaNetLibConcurrency`, `KeyedLockExtensions` | DI and helpers |

## Project Structure

```
StangaNetLib.Concurrency/
├── assets/
│   └── package-icon.png
├── build/
│   └── StangaNetLib.Concurrency.props
├── src/
│   └── StangaNetLib.Concurrency/
│       ├── Configuration/
│       ├── Core/
│       ├── Extensions/
│       ├── Locking/
│       ├── Scheduling/
│       ├── Synchronization/
│       └── Throttling/
└── tests/
    └── StangaNetLib.Concurrency.Tests/
```

## Quick Start

`appsettings.json`:

```json
{
  "ConcurrencySettings": {
    "DefaultThrottleMaxConcurrency": 10,
    "WorkQueueCapacity": 1000,
    "WorkQueueFullMode": "Wait"
  }
}
```

`Program.cs`:

```csharp
builder.Services.AddStangaNetLibConcurrency(builder.Configuration);
```

```csharp
public class OrderService(IKeyedLock keyedLock, IAsyncThrottle throttle)
{
    public async Task ProcessOrderAsync(string orderId)
    {
        await using var _ = await keyedLock.AcquireAsync(orderId);
        await using var slot = await throttle.WaitAsync();
        await _externalApi.CallAsync();
    }
}
```

## IKeyedLock

Per-key async mutual exclusion. Different keys do not block each other; the same key is serialized.

```csharp
await using var handle = await keyedLock.AcquireAsync($"order:{orderId}");

var result = await keyedLock.TryAcquireAsync($"order:{orderId}", TimeSpan.FromSeconds(5));
if (result.IsFailure)
    return Result.Failure<Order>(result.Error); // ConcurrencyErrors.LockTimeout
await using var handle = result.Value;
```

## IAsyncThrottle

Limits concurrent operations via a semaphore. Default instance uses `ConcurrencySettings.DefaultThrottleMaxConcurrency`; use `IAsyncThrottleFactory` for custom limits.

```csharp
await using var slot = await throttle.WaitAsync(cancellationToken);

var imageThrottle = throttleFactory.Create(maxConcurrency: 4);
await using var slot = await imageThrottle.WaitAsync();
```

## IWorkQueue<T>

Bounded async producer-consumer queue backed by `System.Threading.Channels`.

```csharp
await workQueue.EnqueueAsync(new EmailJob(to, subject, body));

var result = workQueue.TryEnqueue(job);
if (result.IsFailure) // ConcurrencyErrors.QueueFull or QueueCompleted
    _logger.LogWarning("Queue full, job dropped");

await foreach (var job in workQueue.ConsumeAllAsync(stoppingToken))
    await _mailer.SendAsync(job);

workQueue.Complete();
```

## IDebouncer

Coalesces rapid triggers so the action runs once after a quiet period.

```csharp
debouncer.Debounce(
    key: $"search:{userId}",
    action: async ct => await _searchService.IndexAsync(query, ct),
    delay: TimeSpan.FromMilliseconds(300));
```

## IAtomicCounter / IAtomicCounterFactory

Lock-free 64-bit counters using `Interlocked`.

```csharp
var requestCounter = counterFactory.GetOrCreate("http.requests");
requestCounter.Increment();

var localCounter = counterFactory.Create(initialValue: 100);
if (localCounter.TryUpdate(expectedValue: 90, newValue: 0))
    _logger.LogInformation("Counter reset to zero");
```

## Configuration

| Key | Type | Default | Description |
|---|---|---|---|
| `DefaultThrottleMaxConcurrency` | `int` | `10` | Max concurrent slots for the default `IAsyncThrottle` |
| `WorkQueueCapacity` | `int` | `1000` | Max items in the default `IWorkQueue<T>` |
| `WorkQueueFullMode` | `BoundedChannelFullMode` | `Wait` | Behaviour when queue is full |

## DI Registration

```csharp
services.AddStangaNetLibConcurrency(configuration);
services.AddStangaNetLibConcurrency(); // defaults
```

| Interface | Lifetime | Notes |
|---|---|---|
| `IKeyedLock` | Singleton | |
| `IKeyedLockFactory` | Singleton | Creates independent keyed locks |
| `IAsyncThrottle` | Singleton | Uses `DefaultThrottleMaxConcurrency` |
| `IAsyncThrottleFactory` | Singleton | Creates independent throttles |
| `IWorkQueue<T>` | Singleton (per T) | Open generic |
| `IWorkQueueFactory` | Singleton | Creates custom queues |
| `IDebouncer` | Singleton | |
| `IAtomicCounterFactory` | Singleton | |

## CI/CD

On push/PR to `main` and `workflow_dispatch`, the shared StangaNet workflow:

1. Restores, builds, and tests on .NET 8 and .NET 9
2. Packs the NuGet package (with symbols)
3. Publishes to GitHub Packages outside pull requests
4. Uploads the `.nupkg` as a build artifact
