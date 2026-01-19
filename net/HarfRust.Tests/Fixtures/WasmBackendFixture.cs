using HarfRust.Wasmtime;

namespace HarfRust.Tests;

/// <summary>
/// Test fixture using the Wasmtime (WASM) backend.
/// </summary>
public class WasmBackendFixture : BackendFixture
{
    private readonly WasmtimeBackend _backend;

    public WasmBackendFixture()
    {
        // Load WASM from file path since tests may not have embedded resource
        var wasmPath = Path.Combine(
            AppContext.BaseDirectory, 
            "..", "..", "..", "..", "..", "rust", "target", "wasm32-wasip1", "release", "harfrust_ffi.wasm"
        );
        
        if (!File.Exists(wasmPath))
        {
            // Try relative to project
            wasmPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "rust", "target", "wasm32-wasip1", "release", "harfrust_ffi.wasm"
            ));
        }

        if (!File.Exists(wasmPath))
        {
            throw new FileNotFoundException(
                $"WASM file not found. Build with: cargo build --release --target wasm32-wasip1. Searched: {wasmPath}");
        }

        _backend = new WasmtimeBackend(wasmPath);
    }

    public override IHarfRustBackend Backend => _backend;

    public override void Dispose()
    {
        _backend.Dispose();
        base.Dispose();
    }
}
