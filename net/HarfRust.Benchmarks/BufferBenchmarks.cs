using BenchmarkDotNet.Attributes;
using HarfRust.Wasmtime;
using HarfRust.Bindings;

namespace HarfRust.Benchmarks;

[MemoryDiagnoser]
public class BufferBenchmarks
{
    private WasmtimeBackend _wasmBackend = null!;
    private NativeBackend _nativeBackend = null!;

    [GlobalSetup]
    public void Setup()
    {
        _wasmBackend = new WasmtimeBackend();
        _nativeBackend = new NativeBackend();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _wasmBackend.Dispose();
    }

    [Benchmark]
    public void Native_CreateAndPopulateBuffer() => CreateAndPopulateBuffer(_nativeBackend);
    [Benchmark]
    public void Wasm_CreateAndPopulateBuffer() => CreateAndPopulateBuffer(_wasmBackend);
    
    private void CreateAndPopulateBuffer(IHarfRustBackend backend)
    {
        HarfRustBackend.Current = backend;
        using var buffer = new HarfRustBuffer();
        buffer.AddString("Hello World");
        buffer.GuessSegmentProperties();        
    }
}
