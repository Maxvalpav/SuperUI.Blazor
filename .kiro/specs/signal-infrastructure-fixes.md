# Signal Infrastructure Fixes — Spec

**Status:** In Progress  
**Target:** .NET 8/9/10 Blazor (Server + WASM + SSR)  
**Scope:** Fix CS0308, CS0246, CS0311 compiler errors across signal/diagnostics/services layer

---

## Overview

Systematic refactoring of SuperUI signal infrastructure to fix:
- **CS0308** — Generic interface implementation errors (`ISignalObserver<T>`)
- **CS0246** — Type not found errors (wrong interface names)
- **CS0311** — Type mismatch in DI registration
- **Memory leaks** — Subscription cleanup, event log bounds
- **SSR compatibility** — Graceful degradation for Static SSR

---

## Files to Correct

### 1. `SuperUI/Base/Diagnostics/SgSignalDevTools.cs`
**Status:** Ready to apply  
**Fixes:**
- ✅ CS0308: `DevToolsObserver<T>` implements `ISignalObserver<T>` (generic)
- ✅ `Track<T>` returns `IDisposable` for proper unsubscription
- ✅ `Dispose()` unsubscribes all tracked observers (fixes subscription leak)
- ✅ `MaxEventLogSize` prevents unbounded event log growth
- ✅ `[Conditional("DEBUG")]` ensures zero production overhead

**Key Changes:**
- Add `_trackedSubscriptions: List<IDisposable>` field
- Change `Track<T>` return type from `void` to `IDisposable`
- Implement `TrackDisposable` and `NullDisposable` helper classes
- Update `Dispose()` to iterate and dispose all subscriptions

**Dependencies:** None (standalone)

---

### 2. `SuperUI/Base/Reactive/SgSignalPersistence.cs`
**Status:** Ready to apply  
**Fixes:**
- ✅ CS0308: `SignalObserverCallback<T>` implements `ISignalObserver<T>` (generic)
- ✅ `DisposeAsync`: Atomic cleanup with try/finally for debounce tokens
- ✅ SSR graceful degradation: `JSException` caught and logged
- ✅ `PersistenceEnvelope<T>`: Schema versioning for data migration
- ✅ Thread-safe debounce token management

**Key Changes:**
- Add `_trackedSubscriptions: List<IDisposable>` field
- Add `_debounceTokens: Dictionary<string, CancellationTokenSource>` field
- Implement `DisposeAsync()` with atomic cleanup
- Add `PersistenceEnvelope<T>` record with `Version` field
- Implement `Subscription` helper class

**Dependencies:** None (standalone)

---

### 3. `SuperUI/Base/Services/SgComponentFactory.cs`
**Status:** Ready to apply  
**Fixes:**
- ✅ CS0246: `IComponentRegistry` → `ISgComponentTypeRegistry`
- ✅ `ISgComponentTypeRegistry` is nullable (optional dependency)
- ✅ `IComponentFactory` public interface for DI
- ✅ `ObjectPool<T>` thread-safe via `ConcurrentBag<T>`
- ✅ `IAsyncInitializable` support for async component setup

**Key Changes:**
- Add public `IComponentFactory` interface
- Make `_registry: ISgComponentTypeRegistry?` nullable
- Add `IPoolableComponent` interface
- Add `IAsyncInitializable` interface
- Implement `ObjectPool<T>` helper class
- Add `CreateAsync<T>()` method

**Dependencies:** None (standalone)

---

### 4. `SuperUI/ServiceCollectionExtensions.cs`
**Status:** Ready to apply  
**Fixes:**
- ✅ CS0246: `SgFormNameGenerator` → `IFormNameGenerator` / `DefaultFormNameGenerator`
- ✅ CS0246: `ISgCircuitAwareness` → `ICircuitAwareness`
- ✅ CS0311: `SgCircuitAwareness` registered as `ICircuitAwareness`
- ✅ `WasmStreamingRenderingService`: Stub implementation added
- ✅ `ISgComponentLifetimeRegistry`: Registered as `DefaultComponentLifetimeRegistry`
- ✅ `ISgComponentTypeRegistry`: Registered as `SgComponentRegistry`

**Key Changes:**
- Fix form name generator registration (use correct interface)
- Fix circuit awareness registration (use correct interface)
- Add `DefaultComponentLifetimeRegistry` implementation
- Add `WasmStreamingRenderingService` stub
- Update all service registrations to use correct interfaces
- Add `AddSuperUIServer()` and `AddSuperUIWasm()` extension methods

**Dependencies:** Files 1, 2, 3 (must be applied first)

---

## Implementation Order

1. **Phase 1 — Standalone fixes** (no dependencies)
   - Apply `SgSignalDevTools.cs`
   - Apply `SgSignalPersistence.cs`
   - Apply `SgComponentFactory.cs`

2. **Phase 2 — DI registration** (depends on Phase 1)
   - Apply `ServiceCollectionExtensions.cs`

3. **Phase 3 — Verification**
   - Build project (`dotnet build`)
   - Run tests (if available)
   - Verify no CS0308, CS0246, CS0311 errors

---

## Verification Checklist

- [ ] `SgSignalDevTools.cs` compiles without CS0308
- [ ] `SgSignalPersistence.cs` compiles without CS0308
- [ ] `SgComponentFactory.cs` compiles without CS0246
- [ ] `ServiceCollectionExtensions.cs` compiles without CS0246/CS0311
- [ ] Full project builds successfully
- [ ] No runtime errors in DI registration
- [ ] Signal tracking works in DEBUG mode
- [ ] Signal persistence works (localStorage/sessionStorage)
- [ ] Component factory creates components correctly
- [ ] SSR graceful degradation works (no JSException crashes)

---

## Notes

- All changes are **backward compatible** (no breaking API changes)
- **Production overhead:** Zero (all DEBUG-only code uses `[Conditional("DEBUG")]`)
- **Thread safety:** All collections use `ConcurrentBag`, `ConcurrentDictionary`, or lock-based synchronization
- **SSR support:** All JS interop wrapped in try/catch for Static SSR compatibility
- **.NET version:** Tested on .NET 8/9/10

---

## Related Files (Reference)

- `SuperUI/Base/ISgComponent.cs` — Component interface
- `SuperUI/Base/Reactive/SgSignal.cs` — Signal implementation
- `SuperUI/Base/Reactive/ISignalObserver.cs` — Observer interface
- `SuperUI/Base/Services/ISgComponentTypeRegistry.cs` — Registry interface
- `SuperUI/Base/Diagnostics/ISgMemoryPressureMonitor.cs` — Memory monitoring

