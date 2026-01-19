using WasmModule = Wasmtime.Module;

namespace HarfRust.Wasmtime;

/// <summary>
/// Wasmtime-based backend for HarfRust text shaping.
/// </summary>
/// <remarks>
/// This backend runs the harfrust library as a WASM module using Wasmtime.
/// It provides a sandboxed, cross-platform alternative to native FFI.
/// </remarks>
public sealed class WasmtimeBackend : IHarfRustBackend, IDisposable
{
    private readonly global::Wasmtime.Engine _engine;
    private readonly WasmModule _module;
    private readonly global::Wasmtime.Linker _linker;
    private readonly WasmContext _context;
    private readonly IHarfRustBackend _previousBackend;
    private bool _disposed;

    /// <summary>
    /// Creates a new Wasmtime backend using the embedded WASM module.
    /// </summary>
    public WasmtimeBackend()
    {
        _engine = new global::Wasmtime.Engine();
        
        // Load embedded WASM module
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("harfrust_ffi.wasm")
            ?? throw new InvalidOperationException("Embedded harfrust_ffi.wasm not found.");
        
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var wasmBytes = ms.ToArray();
        
        _module = WasmModule.FromBytes(_engine, "harfrust", wasmBytes);
        _linker = new global::Wasmtime.Linker(_engine);
        
        // Add WASI support for the module
        _linker.DefineWasi();
        
        // Create shared context
        _context = new WasmContext(this);
        
        _previousBackend = HarfRustBackend.Current;
        HarfRustBackend.Current = this;
    }

    /// <summary>
    /// Creates a new Wasmtime backend from a WASM file path.
    /// </summary>
    /// <param name="wasmPath">Path to the harfrust_ffi.wasm file.</param>
    public WasmtimeBackend(string wasmPath)
    {
        _engine = new global::Wasmtime.Engine();
        _module = WasmModule.FromFile(_engine, wasmPath);
        _linker = new global::Wasmtime.Linker(_engine);
        _linker.DefineWasi();

        _context = new WasmContext(this);
        
        _previousBackend = HarfRustBackend.Current;
        HarfRustBackend.Current = this;
    }

    /// <inheritdoc />
    public IBackendBuffer CreateBuffer()
    {
        ThrowIfDisposed();
        return new WasmBuffer(_context);
    }

    /// <inheritdoc />
    public IBackendFont CreateFont(byte[] data)
    {
        ThrowIfDisposed();
        return new WasmFont(_context, data, 0);
    }

    /// <inheritdoc />
    public IBackendFont CreateFont(byte[] data, uint index)
    {
        ThrowIfDisposed();
        return new WasmFont(_context, data, index);
    }

    internal WasmContext Context => _context;

    /// <summary>
    /// Creates a new store with WASI configuration.
    /// </summary>
    internal global::Wasmtime.Store CreateStore()
    {
        var store = new global::Wasmtime.Store(_engine);
        var wasiConfig = new global::Wasmtime.WasiConfiguration();
        store.SetWasiConfiguration(wasiConfig);
        return store;
    }

    /// <summary>
    /// Instantiates the module in the given store.
    /// </summary>
    internal global::Wasmtime.Instance CreateInstance(global::Wasmtime.Store store)
    {
        return _linker.Instantiate(store, _module);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WasmtimeBackend));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _module.Dispose();
            _engine.Dispose();
            HarfRustBackend.Current = _previousBackend;
            _disposed = true;
        }
    }
}
