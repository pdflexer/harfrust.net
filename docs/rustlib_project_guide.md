# Rust <-> .NET FFI Project Guide

This document outlines the recommended structure and implementation checklist for creating high-performance .NET bindings for Rust libraries. It is designed to be generic and applicable to any project wrapping Rust code for .NET consumption.

## 1. Project Structure

A clean repository structure separates the Native (Rust) and Managed (.NET) concerns while enabling multiple backends (Native & Wasm) and easy cross-compilation.

```text
root/
├── .github/
│   └── workflows/          # CI/CD (Build & Pack)
├── rust/                   # The Rust Crate (FFI Layer)
│   ├── src/
│   │   └── lib.rs          # extern "C" FFI export functions
│   ├── Cargo.toml          # crate-type = ["cdylib"]
│   └── build.rs            # csbindgen configuration
├── net/                    # The .NET Solution
│   ├── MyLibrary/              # Main Library & Default Backend
│   │   ├── MyLibrary.csproj    # Main Package
│   │   ├── MyLibraryFont.cs    # High-level API (Example)
│   │   └── Bindings/           # Generated low-level bindings (FFI)
│   ├── MyLibrary.Wasmtime/     # Alternative Wasm Backend
│   │   └── WasmContext.cs      # Wasmtime implementation
│   ├── MyLibrary.Native/       # Native Assets Package
│   │   └── MyLibrary.Native.csproj # Packs .dll/.so/.dylib
│   └── MyLibrary.Tests/        # Shared Unit Tests
└── docs/                   # Documentation
```

## 2. Implementation Checklist

### Phase 1: Native Setup (Rust)

- [ ] **Crate Configuration**: Set `crate-type = ["cdylib"]` in `Cargo.toml`.
- [ ] **FFI Exports**: Expose functions as `#[no_mangle] pub unsafe extern "C"`.
- [ ] **Opaque Types**: Use opaque pointers (e.g., `*mut MyStruct`) to hide Rust implementation details from C#.
- [ ] **Memory Management**: Provide explicit `_new()` and `_free()` functions for all heap-allocated objects.
- [ ] **Error Handling**: Don't panic across FFI boundaries. Return integer error codes or use thread-local error info.
- [ ] **Build Optimization**: configure `[profile.release]` with `lto = true` and `panic = "abort"`.

### Phase 2: Bindings (Auto-Generation)

- [ ] **Tooling**: Use `csbindgen` (recommended) or `bindgen`.
- [ ] **Configuration**: Add `csbindgen` to `[build-dependencies]` and configure `build.rs` to generate C# files directly into the .NET project (e.g. `../net/MyLibrary/Bindings/NativeMethods.g.cs`).
- [ ] **Regeneration**: Ensure bindings are updated whenever `cargo build` is run.

### Phase 3: Managed Wrapper (.NET)

- [ ] **Structure**: Consolidate bindings into a `Bindings` subfolder within the main project (internal access).
- [ ] **Safe Handles**: Wrap native pointers in `SafeHandle` or standard `IDisposable` classes.
- [ ] **Zero-Allocation**: Use `San<T>`, `ReadOnlySpan<T>`, and `fixed` statements to pass data without copying.
- [ ] **UTF-8/UTF-16**:
    - Use Rust's standard string handling (`CStr`, `String`).
    - For high performance, consider native UTF-16 APIs on the Rust side to avoid conversion overhead in .NET.
- [ ] **Platform Handling**: Use `copy` instructions in `.csproj` to ensure native binaries (`.dll`, `.so`, `.dylib`) are available during development.
- [ ] **Wasmtime Backend (Optional)**:
    - **WASM Embedding**: Embed the `.wasm` file as an `EmbeddedResource` in the backend project.
    - **Memory Management**: Implement explicit `Malloc`/`Free` exports in Rust and use them to marshal data into the WASM linear memory.
    - **Context Management**: Create a `WasmContext` class to manage the `Wasmtime.Store`, `Instance`, and `Memory` exports.
    - **Function Mapping**: Use `Instance.GetFunction<T...>` to map exported Rust functions to C# delegates.
    - **Serialization**: Be aware that complex structs cannot be passed directly to WASM; pass pointers (int offsets) to data you've written to WASM memory.

### Phase 4: Packaging (NuGet)

- [ ] **Folder Structure**: Map native assets to `runtimes/{rid}/native/`.
    - `win-x64` -> `runtimes/win-x64/native/mylib_ffi.dll`
    - `linux-x64` -> `runtimes/linux-x64/native/libmylib_ffi.so`
    - `osx-x64`/`osx-arm64` -> `runtimes/osx...`
- [ ] **Conditionals**: Use `Condition="Exists(...)"` in `.csproj` so the package builds even if some native binaries are missing locally (allowing cross-platform CI to fill them in).

## 3. Testing Strategy
 
 To ensure consistency across multiple backends (Native and Wasm), use a shared test suite based on xUnit Class Fixtures.
 
 ### Fixture Pattern
 
 1. **Core Abstractions**:
    - `BackendFixture`: Abstract base class managing shared resources and defining the abstract `Backend` property.
    - `ILibraryBackend`: The common interface implemented by both `NativeBackend` and `WasmtimeBackend`.

 2. **Concrete Fixtures**:
    - `NativeBackendFixture` : `BackendFixture`: Instantiates the native P/Invoke backend (`NativeBackend`).
    - `WasmBackendFixture` : `BackendFixture`: Instantiates the Wasmtime backend.
 
 3. **Shared Test Classes**:
    - Write tests in an abstract base class `ShapingTestsBase<TFixture>`.
    - Use `IClassFixture<TFixture>` to inject the specific backend.
    - Tests interact only with the `ILibraryBackend` interface.
 
 4. **Test Implementation**:
    ```csharp
    public abstract class ShapingTestsBase<TFixture> : IClassFixture<TFixture> 
        where TFixture : BackendFixture
    {
        protected readonly TFixture Fixture;
        protected ILibraryBackend Backend => Fixture.Backend;
        
        // ... shared tests ...
    }
 
    public class NativeTests : TestsBase<NativeBackendFixture> { ... }
    public class WasmTests : TestsBase<WasmBackendFixture> { ... }
    ```
 
 ## 4. CI/CD Pipeline

- [ ] **Matrix Build**: Use separate runners for Windows, Linux, and macOS to build native binaries.
- [ ] **Artifact Staging**: Upload native binaries as build artifacts.
- [ ] **Pack Step**: Download all native artifacts into the expected layout before running `dotnet pack`.
- [ ] **Automation**: Trigger builds on Push/PR and release tag creation.
