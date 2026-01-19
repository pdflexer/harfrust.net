namespace HarfRust.Wasmtime;

/// <summary>
/// Wasmtime-based font implementation.
/// </summary>
internal sealed class WasmFont : IBackendFont
{
    private readonly WasmContext _context;
    private int _handle;
    private bool _disposed;

    public WasmFont(WasmContext context, byte[] data, uint index)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            throw new ArgumentException("Font data cannot be empty.", nameof(data));

        _context = context;
        
        // Copy font data to WASM memory
        var dataPtr = _context.AllocateAndWrite(data);
        try
        {
            _handle = index == 0
                ? _context.FontFromData(dataPtr, data.Length)
                : _context.FontFromDataIndex(dataPtr, data.Length, (int)index);
        }
        finally
        {
            // Note: Font copies the data internally, so we can free this
            _context.Free(dataPtr);
        }

        if (_handle == 0)
        {
            throw new ArgumentException("Invalid font data.", nameof(data));
        }
    }

    internal WasmContext Context => _context;
    internal int Handle => _handle;

    public int UnitsPerEm
    {
        get
        {
            ThrowIfDisposed();
            return _context.FontUnitsPerEm(_handle);
        }
    }

    public IBackendGlyphBuffer Shape(IBackendBuffer buffer)
    {
        ThrowIfDisposed();
        
        // If buffer is WasmBuffer and shares our context, we can use it directly
        if (buffer is WasmBuffer wasmBuffer && wasmBuffer.Context == _context)
        {
            var bufferHandle = (int)wasmBuffer.ConsumeHandle();
            var glyphBufferHandle = _context.Shape(_handle, bufferHandle);
            if (glyphBufferHandle == 0)
            {
                throw new InvalidOperationException("Shaping failed.");
            }
            return new WasmGlyphBuffer(_context, glyphBufferHandle);
        }
        
        // Otherwise we would need to copy (not implemented for now as we control creation)
        throw new ArgumentException("Buffer must be created from the same backend instance.", nameof(buffer));
    }

    public IBackendGlyphBuffer Shape(IBackendBuffer buffer, Feature[]? features, Variation[]? variations)
    {
        ThrowIfDisposed();

        if ((features == null || features.Length == 0) && (variations == null || variations.Length == 0))
        {
            return Shape(buffer);
        }

        if (buffer is not WasmBuffer wasmBuffer || wasmBuffer.Context != _context)
        {
             throw new ArgumentException("Buffer must be created from the same backend instance.", nameof(buffer));
        }

        var bufferHandle = (int)wasmBuffer.ConsumeHandle();

        int featuresPtr = 0;
        int variationsPtr = 0;

        try
        {
            // Allocate and copy features if present
            if (features != null && features.Length > 0)
            {
                var featureBytes = new byte[features.Length * 16]; // 4 uints per feature
                for (int i = 0; i < features.Length; i++)
                {
                    var offset = i * 16;
                    BitConverter.TryWriteBytes(featureBytes.AsSpan(offset), features[i].Tag);
                    BitConverter.TryWriteBytes(featureBytes.AsSpan(offset + 4), features[i].Value);
                    BitConverter.TryWriteBytes(featureBytes.AsSpan(offset + 8), features[i].Start);
                    BitConverter.TryWriteBytes(featureBytes.AsSpan(offset + 12), features[i].End);
                }
                featuresPtr = _context.AllocateAndWrite(featureBytes);
            }

            // Allocate and copy variations if present
            if (variations != null && variations.Length > 0)
            {
                var variationBytes = new byte[variations.Length * 8]; // uint + float per variation
                for (int i = 0; i < variations.Length; i++)
                {
                    var offset = i * 8;
                    BitConverter.TryWriteBytes(variationBytes.AsSpan(offset), variations[i].Tag);
                    BitConverter.TryWriteBytes(variationBytes.AsSpan(offset + 4), variations[i].Value);
                }
                variationsPtr = _context.AllocateAndWrite(variationBytes);
            }

            var glyphBufferHandle = _context.ShapeFull(
                _handle,
                (int)bufferHandle,
                featuresPtr,
                features == null ? 0 : features.Length,
                variationsPtr,
                variations == null ? 0 : variations.Length
            );

            if (glyphBufferHandle == 0)
            {
                throw new InvalidOperationException("Shaping failed.");
            }

            return new WasmGlyphBuffer(_context, glyphBufferHandle);
        }
        finally
        {
            if (featuresPtr != 0) _context.Free(featuresPtr);
            if (variationsPtr != 0) _context.Free(variationsPtr);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmFont));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != 0)
            {
                _context.FontFree(_handle);
                _handle = 0;
            }
            // Do NOT dispose _context as it is owned by the backend
            _disposed = true;
        }
    }
}
