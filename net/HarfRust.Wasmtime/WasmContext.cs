namespace HarfRust.Wasmtime;

/// <summary>
/// Manages interactions with a single WASM instance.
/// </summary>
internal sealed class WasmContext : IDisposable
{
    private readonly global::Wasmtime.Store _store;
    private readonly global::Wasmtime.Instance _instance;
    private readonly global::Wasmtime.Memory _memory;
    
    // Function exports
    private readonly Func<int> _bufferNew;
    private readonly Func<int, int, int, int> _bufferAddUtf16;
    private readonly Func<int, int> _bufferLen;
    private readonly Action<int> _bufferClear;
    private readonly Action<int> _bufferFree;
    private readonly Action<int, int> _bufferSetDirection;
    private readonly Func<int, int> _bufferGetDirection;
    private readonly Action<int, int> _bufferSetScript;
    private readonly Func<int, int> _bufferGetScript;
    private readonly Func<int, int, int> _bufferSetLanguage;
    private readonly Action<int> _bufferGuessSegmentProperties;
    private readonly Func<int, int, int> _fontFromData;
    private readonly Func<int, int, int, int> _fontFromDataIndex;
    private readonly Func<int, int> _fontUnitsPerEm;
    private readonly Action<int> _fontFree;
    private readonly Func<int, int, int> _shape;
    private readonly Func<int, int, int, int, int, int, int> _shapeFull;
    private readonly Func<int, int> _glyphBufferLen;
    private readonly Func<int, int> _glyphBufferGetInfos;
    private readonly Func<int, int> _glyphBufferGetPositions;
    private readonly Func<int, int> _glyphBufferIntoBuffer;
    private readonly Action<int> _glyphBufferFree;
    private readonly Func<int, int> _malloc;
    private readonly Action<int> _free;

    private bool _disposed;

    public WasmContext(WasmtimeBackend backend)
    {
        _store = backend.CreateStore();
        _instance = backend.CreateInstance(_store);
        
        // Get memory export
        _memory = _instance.GetMemory("memory")
            ?? throw new InvalidOperationException("WASM module has no 'memory' export.");
        
        // Get function exports
        _bufferNew = _instance.GetFunction<int>("harfrust_buffer_new")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_new");
        _bufferAddUtf16 = _instance.GetFunction<int, int, int, int>("harfrust_buffer_add_utf16")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_add_utf16");
        _bufferLen = _instance.GetFunction<int, int>("harfrust_buffer_len")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_len");
        _bufferClear = _instance.GetAction<int>("harfrust_buffer_clear")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_clear");
        _bufferFree = _instance.GetAction<int>("harfrust_buffer_free")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_free");
        _bufferSetDirection = _instance.GetAction<int, int>("harfrust_buffer_set_direction")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_set_direction");
        _bufferGetDirection = _instance.GetFunction<int, int>("harfrust_buffer_get_direction")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_get_direction");
        _bufferSetScript = _instance.GetAction<int, int>("harfrust_buffer_set_script")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_set_script");
        _bufferGetScript = _instance.GetFunction<int, int>("harfrust_buffer_get_script")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_get_script");
        _bufferSetLanguage = _instance.GetFunction<int, int, int>("harfrust_buffer_set_language")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_set_language");
        _bufferGuessSegmentProperties = _instance.GetAction<int>("harfrust_buffer_guess_segment_properties")
            ?? throw new InvalidOperationException("Missing export: harfrust_buffer_guess_segment_properties");
        _fontFromData = _instance.GetFunction<int, int, int>("harfrust_font_from_data")
            ?? throw new InvalidOperationException("Missing export: harfrust_font_from_data");
        _fontFromDataIndex = _instance.GetFunction<int, int, int, int>("harfrust_font_from_data_index")
            ?? throw new InvalidOperationException("Missing export: harfrust_font_from_data_index");
        _fontUnitsPerEm = _instance.GetFunction<int, int>("harfrust_font_units_per_em")
            ?? throw new InvalidOperationException("Missing export: harfrust_font_units_per_em");
        _fontFree = _instance.GetAction<int>("harfrust_font_free")
            ?? throw new InvalidOperationException("Missing export: harfrust_font_free");
        _shape = _instance.GetFunction<int, int, int>("harfrust_shape")
            ?? throw new InvalidOperationException("Missing export: harfrust_shape");
        _shapeFull = _instance.GetFunction<int, int, int, int, int, int, int>("harfrust_shape_full")
            ?? throw new InvalidOperationException("Missing export: harfrust_shape_full");
        _glyphBufferLen = _instance.GetFunction<int, int>("harfrust_glyph_buffer_len")
            ?? throw new InvalidOperationException("Missing export: harfrust_glyph_buffer_len");
        _glyphBufferGetInfos = _instance.GetFunction<int, int>("harfrust_glyph_buffer_get_infos")
            ?? throw new InvalidOperationException("Missing export: harfrust_glyph_buffer_get_infos");
        _glyphBufferGetPositions = _instance.GetFunction<int, int>("harfrust_glyph_buffer_get_positions")
            ?? throw new InvalidOperationException("Missing export: harfrust_glyph_buffer_get_positions");
        _glyphBufferIntoBuffer = _instance.GetFunction<int, int>("harfrust_glyph_buffer_into_buffer")
            ?? throw new InvalidOperationException("Missing export: harfrust_glyph_buffer_into_buffer");
        _glyphBufferFree = _instance.GetAction<int>("harfrust_glyph_buffer_free")
            ?? throw new InvalidOperationException("Missing export: harfrust_glyph_buffer_free");
        
        // Memory allocation functions
        _malloc = _instance.GetFunction<int, int>("harfrust_alloc")
            ?? throw new InvalidOperationException("Missing export: harfrust_alloc");
        _free = _instance.GetAction<int>("harfrust_dealloc")
            ?? throw new InvalidOperationException("Missing export: harfrust_dealloc");
    }

    public global::Wasmtime.Memory Memory => _memory;

    // Buffer operations
    public int BufferNew() => _bufferNew();
    public int BufferAddUtf16(int buffer, int textPtr, int len) => _bufferAddUtf16(buffer, textPtr, len);
    public int BufferLen(int buffer) => _bufferLen(buffer);
    public void BufferClear(int buffer) => _bufferClear(buffer);
    public void BufferFree(int buffer) => _bufferFree(buffer);
    public void BufferSetDirection(int buffer, int direction) => _bufferSetDirection(buffer, direction);
    public int BufferGetDirection(int buffer) => _bufferGetDirection(buffer);
    public void BufferSetScript(int buffer, int script) => _bufferSetScript(buffer, script);
    public int BufferGetScript(int buffer) => _bufferGetScript(buffer);
    public int BufferSetLanguage(int buffer, int langPtr) => _bufferSetLanguage(buffer, langPtr);
    public void BufferGuessSegmentProperties(int buffer) => _bufferGuessSegmentProperties(buffer);

    // Font operations
    public int FontFromData(int dataPtr, int len) => _fontFromData(dataPtr, len);
    public int FontFromDataIndex(int dataPtr, int len, int index) => _fontFromDataIndex(dataPtr, len, index);
    public int FontUnitsPerEm(int font) => _fontUnitsPerEm(font);
    public void FontFree(int font) => _fontFree(font);

    // Shape operations
    public int Shape(int font, int buffer) => _shape(font, buffer);
    public int ShapeFull(int font, int buffer, int featuresPtr, int numFeatures, int variationsPtr, int numVariations)
        => _shapeFull(font, buffer, featuresPtr, numFeatures, variationsPtr, numVariations);

    // Glyph buffer operations
    public int GlyphBufferLen(int buffer) => _glyphBufferLen(buffer);
    public int GlyphBufferGetInfos(int buffer) => _glyphBufferGetInfos(buffer);
    public int GlyphBufferGetPositions(int buffer) => _glyphBufferGetPositions(buffer);
    public int GlyphBufferIntoBuffer(int buffer) => _glyphBufferIntoBuffer(buffer);
    public void GlyphBufferFree(int buffer) => _glyphBufferFree(buffer);

    // Memory operations
    public int Malloc(int size) => _malloc(size);
    public void Free(int ptr) => _free(ptr);

    /// <summary>
    /// Writes bytes to WASM memory at the given offset.
    /// </summary>
    public void WriteBytes(int offset, ReadOnlySpan<byte> data)
    {
        var span = _memory.GetSpan<byte>(offset, data.Length);
        data.CopyTo(span);
    }

    /// <summary>
    /// Reads bytes from WASM memory at the given offset.
    /// </summary>
    public ReadOnlySpan<byte> ReadBytes(int offset, int length)
    {
        return _memory.GetSpan<byte>(offset, length);
    }

    /// <summary>
    /// Allocates memory in WASM and copies data to it.
    /// </summary>
    public int AllocateAndWrite(byte[] data)
    {
        var ptr = Malloc(data.Length);
        if (ptr == 0)
            throw new OutOfMemoryException("Failed to allocate WASM memory.");
        WriteBytes(ptr, data);
        return ptr;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _store.Dispose();
            _disposed = true;
        }
    }
}
