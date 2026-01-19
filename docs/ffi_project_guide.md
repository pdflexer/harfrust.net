# Rust <-> .NET FFI Project Guide

This document outlines the recommended structure and implementation checklist for creating high-performance .NET bindings for Rust libraries. It is designed to be generic and applicable to any project wrapping Rust code for .NET consumption.

## 1. Project Structure

A clean repository structure separates the Native (Rust) and Managed (.NET) concerns while enabling easy cross-compilation and packaging.

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
│   ├── MyLibrary/              # Main .NET Library (Public API & Bindings)
│   │   ├── MyLibrary.csproj    # Packages native assets
│   │   ├── Wrapper.cs          # Safe Handle wrappers
│   │   └── Bindings/           # Generated low-level bindings
│   │       └── NativeMethods.g.cs
│   └── MyLibrary.Tests/        # Unit Tests
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

### Phase 4: Packaging (NuGet)

- [ ] **Folder Structure**: Map native assets to `runtimes/{rid}/native/`.
    - `win-x64` -> `runtimes/win-x64/native/mylib.dll`
    - `linux-x64` -> `runtimes/linux-x64/native/libmylib.so`
    - `osx-x64`/`osx-arm64` -> `runtimes/osx...`
- [ ] **Conditionals**: Use `Condition="Exists(...)"` in `.csproj` so the package builds even if some native binaries are missing locally (allowing cross-platform CI to fill them in).

## 3. CI/CD Pipeline

- [ ] **Matrix Build**: Use separate runners for Windows, Linux, and macOS to build native binaries.
- [ ] **Artifact Staging**: Upload native binaries as build artifacts.
- [ ] **Pack Step**: Download all native artifacts into the expected layout before running `dotnet pack`.
- [ ] **Automation**: Trigger builds on Push/PR and release tag creation.
