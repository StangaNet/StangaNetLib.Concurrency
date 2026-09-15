# StangaNetLib.Concurrency

Simplified threading, multi-threading and concurrent data access utilities for .NET: keyed locks, async throttling, bounded work queues, debouncing, and atomic counters.

[![CI](https://github.com/StangaNet/StangaNetLib.Concurrency/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Concurrency/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.1-blue)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209-512BD4)](https://dotnet.microsoft.com)

## Overview

`StangaNetLib.Concurrency` provides high-performance, lock-free, and semaphore-based primitives for managing concurrency in modern .NET applications. It focuses on domain-centric primitives that are easy to use and highly reliable.

## Design Philosophy

*   **Domain-Centric**: Primitives are organized by their functional purpose (e.g., `Locking`, `Throttling`) rather than technical implementation details.
*   **Functional Error Handling**: Uses `Result<T>` for expected business logic failures (e.g., timeouts), reserving exceptions for catastrophic system errors.
*   **Defensive & Robust**: Built with strong guard clauses and thread-safe primitives to ensure reliability in high-contention scenarios.
*   **Modern .NET**: Fully optimized for .NET 8/9, using `IAsyncDisposable`, `ValueTask`, and modern C# features.

## Installation

Add the GitHub Packages feed to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

Add the package to your `.csproj`:

```xml
<PackageReference Include="StangaNetLib.Concurrency" Version="1.0.1" />
```

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Concurrency.Locking` | `IKeyedLock`, `IKeyedLockFactory` | Per-resource async mutual exclusion |
| `StangaNetLib.Concurrency.Throttling` | `IAsyncThrottle`, `IAsyncThrottleFactory` | Semaphore-based concurrency limiter |
| `StangaNetLib.Concurrency.Synchronization` | `IWorkQueue<T>`, `IWorkQueueFactory` | Bounded async producer-consumer queue |
| `StangaNetLib.Concurrency.Scheduling` | `IDebouncer` | Per-key debouncing of async actions |
| `StangaNetLib.Concurrency.Synchronization` | `IAtomicCounter`, `IAtomicCounterFactory` | Lock-free thread-safe 64-bit counters |
| `StangaNetLib.Concurrency` | `ConcurrencyErrors` | Typed `Error` constants |
| `StangaNetLib.Concurrency.Configuration` | `ConcurrencySettings` | Configuration POCO |

## Project Structure

```
StangaNetLib.Concurrency/
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

Inject and use:

```csharp
public class OrderService(IKeyedLock keyedLock, IAsyncThrottle throttle)
{
    public async Task ProcessOrderAsync(string orderId)
    {
        // Serialize per-order operations without blocking other orders
        await using var _ = await keyedLock.AcquireAsync(orderId);

        // Limit concurrent calls to a downstream API
        await using var slot = await throttle.WaitAsync();
        await _externalApi.CallAsync();
    }
}
```

## IKeyedLock

Provides per-key async mutual exclusion. Concurrent calls with **different keys** proceed without blocking each other; calls with the **same key** are serialized.

```csharp
// Always acquires (waits indefinitely)
await using var handle = await keyedLock.AcquireAsync($"order:{orderId}");
// critical section

// With timeout — returns Result<IAsyncDisposable>
var result = await keyedLock.TryAcquireAsync($"order:{orderId}", TimeSpan.FromSeconds(5));
if (result.IsFailure)
    return Result.Failure<Order>(result.Error); // ConcurrencyErrors.LockTimeout
await using var handle = result.Value;
```

## IAsyncThrottle

Limits the number of concurrent operations via a semaphore. The default instance is configured by `ConcurrencySettings.DefaultThrottleMaxConcurrency`. Use `IAsyncThrottleFactory` for custom limits.

```csharp
// Default throttle (from DI, configured by settings)
await using var slot = await throttle.WaitAsync(cancellationToken);
await ProcessAsync();

// Custom throttle via factory
var imageThrottle = throttleFactory.Create(maxConcurrency: 4);
await using var slot = await imageThrottle.WaitAsync();
await ResizeImageAsync();
```

## IWorkQueue<T>

Bounded async producer-consumer queue backed by `System.Threading.Channels`. Producers and consumers run independently.

```csharp
// Producer
await workQueue.EnqueueAsync(new EmailJob(to, subject, body));

// Non-blocking enqueue
var result = workQueue.TryEnqueue(job);
if (result.IsFailure) // ConcurrencyErrors.QueueFull or QueueCompleted
    _logger.LogWarning("Queue full, job dropped");

// Consumer (background service)
await foreach (var job in workQueue.ConsumeAllAsync(stoppingToken))
    await _mailer.SendAsync(job);

// Signal end of production
workQueue.Complete();
```

## IDebouncer

Coalesces rapid triggers so the action runs only once after the last trigger, waiting for a quiet period.

```csharp
// Debounce a search-as-you-type query — fires 300 ms after the user stops typing
debouncer.Debounce(
    key: $"search:{userId}",
    action: async ct => await _searchService.IndexAsync(query, ct),
    delay: TimeSpan.FromMilliseconds(300));
```

Each call with the same `key` resets the timer. Different keys are independent.

## IAtomicCounter / IAtomicCounterFactory

Lock-free 64-bit counters using `Interlocked` operations.

```csharp
// Create a named shared counter
var requestCounter = counterFactory.GetOrCreate("http.requests");
requestCounter.Increment();

// Create an independent counter
var localCounter = counterFactory.Create(initialValue: 100);
localCounter.Add(-10);

// Optimistic compare-and-swap
if (localCounter.TryUpdate(expectedValue: 90, newValue: 0))
    _logger.LogInformation("Counter reset to zero");
```

## Configuration

| Key | Type | Default | Description |
|---|---|---|---|
| `DefaultThrottleMaxConcurrency` | `int` | `10` | Max concurrent slots for the default `IAsyncThrottle` |
| `WorkQueueCapacity` | `int` | `1000` | Max items in the default `IWorkQueue<T>` |
| `WorkQueueFullMode` | `BoundedChannelFullMode` | `Wait` | Behaviour when queue is full (`Wait`, `DropWrite`, `DropOldest`, `DropNewest`) |

## DI Registration

```csharp
// With configuration (recommended)
services.AddStangaNetLibConcurrency(configuration);

// With defaults (no appsettings needed)
services.AddStangaNetLibConcurrency();
```

Registered services:

| Interface | Lifetime | Notes |
|---|---|---|
| `IKeyedLock` | Singleton | |
| `IAsyncThrottle` | Singleton | Uses `DefaultThrottleMaxConcurrency` |
| `IAsyncThrottleFactory` | Singleton | Creates independent throttles |
| `IWorkQueue<T>` | Singleton (per T) | Open generic; one queue per item type |
| `IDebouncer` | Singleton | |
| `IAtomicCounterFactory` | Singleton | |

## CI/CD

The pipeline runs on `workflow_dispatch` and:

1. Restores, builds, and tests on both .NET 8 and .NET 9
2. Packs the NuGet package
3. Publishes to GitHub Packages
4. Uploads the `.nupkg` as a build artifact
