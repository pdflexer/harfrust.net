using BenchmarkDotNet.Attributes;
using HarfRust.Wasmtime;
using HarfRust.Bindings;

namespace HarfRust.Benchmarks;

[MemoryDiagnoser]
public class ShapingBenchmarks
{
    private byte[] _fontData = null!;
    private HarfRustFont _nativeFont = null!;
    private HarfRustFont _wasmFont = null!;
    private WasmtimeBackend _wasmBackend = null!;
    private NativeBackend _nativeBackend = null!;

    [Params(10, 100, 1000)]
    public int TextLength;

    private string _text = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fontData = BenchmarkUtils.GetFontData();

        // Prep fonts once to measure shaping only
        _nativeFont = new HarfRustFont(_fontData);
        
        _nativeBackend = NativeBackend.Instance;
        
        _wasmBackend = new WasmtimeBackend();
        _wasmFont = new HarfRustFont(_fontData, _wasmBackend);

        // Create text
        _text = string.Concat(Enumerable.Repeat("Hello World ", TextLength / 10 + 1)).Substring(0, TextLength);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _nativeFont.Dispose();
        _wasmFont.Dispose();
        _wasmBackend.Dispose();
    }

    [Benchmark]
    public void Native_Shape() => Shape(_nativeFont, _nativeBackend);

    [Benchmark]
    public void Wasm_Shape() => Shape(_wasmFont, _wasmBackend);
    
    private void Shape(HarfRustFont font, IHarfRustBackend backend)
    {
        HarfRustBackend.Current = backend;
        using var buffer = new HarfRustBuffer();
        buffer.AddString(_text);
        buffer.GuessSegmentProperties();
        using var result = font.Shape(buffer); // Font also knows its backend
        var count = result.Length;
    }
}
