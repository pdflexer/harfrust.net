using Xunit;
using WasmEngine = Wasmtime.Engine;

namespace HarfRust.Tests;

public class WasmtimeExternalEngineTests
{
    private static string GetWasmPath()
    {
        var wasmPath = Path.Combine(
            AppContext.BaseDirectory, 
            "..", "..", "..", "..", "..", "rust", "target", "wasm32-wasip1", "release", "harfrust_ffi.wasm"
        );
        
        if (!File.Exists(wasmPath))
        {
            wasmPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "rust", "target", "wasm32-wasip1", "release", "harfrust_ffi.wasm"
            ));
        }

        if (!File.Exists(wasmPath))
        {
            // Fallback for CI or other environments where path might differ
             wasmPath = Path.Combine(AppContext.BaseDirectory, "harfrust_ffi.wasm");
        }
        
        return wasmPath;
    }

    [Fact]
    public void CanShareEngineBetweenBackends()
    {
        using var engine = new WasmEngine();
        var wasmPath = GetWasmPath();
        
        Assert.True(File.Exists(wasmPath), $"WASM file not found at {wasmPath}");

        using (var backend1 = new HarfRust.Wasmtime.WasmtimeBackend(engine, wasmPath))
        using (var backend2 = new HarfRust.Wasmtime.WasmtimeBackend(engine, wasmPath))
        {
            var buffer1 = backend1.CreateBuffer();
            buffer1.AddString("Test 1");
            Assert.Equal(6, buffer1.Length);

            var buffer2 = backend2.CreateBuffer();
            buffer2.AddString("Test 2");
            Assert.Equal(6, buffer2.Length);
        }
        
        // Engine should still be valid/usable (not disposed by backends)
         // There isn't a direct "IsDisposed" on Engine, but we can try to use it
        using (var backend3 = new HarfRust.Wasmtime.WasmtimeBackend(engine, wasmPath))
        {
             var buffer3 = backend3.CreateBuffer();
             Assert.Equal(0, buffer3.Length);
        }
    }

    [Fact]
    public void BackendDoesNotDisposeExternalEngine()
    {
        var engine = new WasmEngine();
        var wasmPath = GetWasmPath();
        
        // Create backend and immediately dispose it
        using (var backend = new HarfRust.Wasmtime.WasmtimeBackend(engine, wasmPath))
        {
        }
        
        // If engine was disposed, this would likely throw or fail
        // Creating another backend with it verifies it's still alive
        using (var backend2 = new HarfRust.Wasmtime.WasmtimeBackend(engine, wasmPath))
        {
            var buffer = backend2.CreateBuffer();
            Assert.Equal(0, buffer.Length);
        }
        
        engine.Dispose();
    }
}
