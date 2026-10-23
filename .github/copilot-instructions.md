# StangaNetLib.Concurrency — Copilot instructions

- This library provides concurrency primitives (keyed locks, throttles, work queues, debouncers, atomic counters) on top of `StangaNetLib.Core`.
- Prefer `Result` / `Result<T>` and `ConcurrencyErrors` for expected failures (timeouts, queue full); reserve exceptions for invalid arguments / catastrophic errors.
- Keep public APIs documented (`///`), thread-safe, and free of host-specific infrastructure.
- DI entry point is `AddStangaNetLibConcurrency` in `StangaNetLib.Concurrency.Extensions`.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover `net8.0`, `net9.0`, and `net10.0` when changing shared behavior.
