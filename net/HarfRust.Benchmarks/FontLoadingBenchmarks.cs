using BenchmarkDotNet.Attributes;
using HarfRust.Wasmtime;
using HarfRust.Bindings;

namespace HarfRust.Benchmarks;

[MemoryDiagnoser]
public class FontLoadingBenchmarks
{
    private byte[] _fontData = null!;
    private WasmtimeBackend _wasm = null!;
    private NativeBackend _native = null!;    

    [GlobalSetup]
    public void Setup()
    {
        _fontData = BenchmarkUtils.GetFontData();
        _wasm = new WasmtimeBackend();
        _native = NativeBackend.Instance;
    }

    [Benchmark]
    public void Native_LoadFont_FromData() => LoadFont(_native);

    [Benchmark]
    public void Wasm_LoadFont_FromData() => LoadFont(_wasm);

    private void LoadFont(IHarfRustBackend backend)
    {
        HarfRustBackend.Current = backend;
        using var font = new HarfRustFont(_fontData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _wasm.Dispose();
    }
}
