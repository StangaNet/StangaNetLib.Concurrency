# Changelog

All notable changes to **StangaNetLib.Concurrency** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Add `net10.0` target framework alongside `net8.0` and `net9.0`.
- Require .NET SDK 10 (global.json) to multi-target `net8.0` / `net9.0` / `net10.0`; opt in Microsoft.Testing.Platform as the `dotnet test` runner.

## [1.0.0] - 2026-10-16

### Added
- Keyed async locks (`IKeyedLock` / `IKeyedLockFactory`) with timeout via `Result`.
- Async throttling (`IAsyncThrottle` / `IAsyncThrottleFactory`).
- Bounded work queues (`IWorkQueue<T>` / `IWorkQueueFactory`).
- Per-key debouncing (`IDebouncer`) and lock-free atomic counters (`IAtomicCounter` / `IAtomicCounterFactory`).
- DI registration (`AddStangaNetLibConcurrency`) and `ConcurrencySettings` binding.
- Typed `ConcurrencyErrors` for expected concurrency failures.
- Dual targeting `net8.0` / `net9.0`, XML docs, symbols (`snupkg`), package icon, and MSBuild consumer metadata stamp.
- Depends on `StangaNetLib.Core` 1.0.0; Microsoft.Extensions.* pinned to 8.x for dual-targeting.
- CI via shared StangaNet reusable workflow (push / PR / manual; publish outside PRs).

[Unreleased]: https://github.com/StangaNet/StangaNetLib.Concurrency/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/StangaNet/StangaNetLib.Concurrency/releases/tag/v1.0.0
