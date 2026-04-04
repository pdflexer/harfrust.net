using System.Buffers;
using System.Buffers.Binary;

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
            _context.Free(dataPtr, data.Length);
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

        return Shape(buffer, features.AsSpan(), variations.AsSpan());
    }

    public IBackendGlyphBuffer Shape(IBackendBuffer buffer, ReadOnlySpan<Feature> features, ReadOnlySpan<Variation> variations = default)
    {
        ThrowIfDisposed();

        if (features.IsEmpty && variations.IsEmpty)
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
        byte[]? rentedFeatures = null;
        byte[]? rentedVariations = null;

        try
        {
            // Allocate and copy features if present
            if (!features.IsEmpty)
            {
                var byteCount = features.Length * 16;
                Span<byte> featureBytes = byteCount <= 256
                    ? stackalloc byte[byteCount]
                    : (rentedFeatures = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

                for (int i = 0; i < features.Length; i++)
                {
                    var offset = i * 16;
                    BinaryPrimitives.WriteUInt32LittleEndian(featureBytes.Slice(offset, 4), features[i].Tag);
                    BinaryPrimitives.WriteUInt32LittleEndian(featureBytes.Slice(offset + 4, 4), features[i].Value);
                    BinaryPrimitives.WriteUInt32LittleEndian(featureBytes.Slice(offset + 8, 4), features[i].Start);
                    BinaryPrimitives.WriteUInt32LittleEndian(featureBytes.Slice(offset + 12, 4), features[i].End);
                }

                featuresPtr = _context.Malloc(byteCount);
                if (featuresPtr == 0)
                {
                    throw new OutOfMemoryException("Failed to allocate WASM memory.");
                }
                _context.WriteBytes(featuresPtr, featureBytes);
            }

            // Allocate and copy variations if present
            if (!variations.IsEmpty)
            {
                var byteCount = variations.Length * 8;
                Span<byte> variationBytes = byteCount <= 256
                    ? stackalloc byte[byteCount]
                    : (rentedVariations = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

                for (int i = 0; i < variations.Length; i++)
                {
                    var offset = i * 8;
                    BinaryPrimitives.WriteUInt32LittleEndian(variationBytes.Slice(offset, 4), variations[i].Tag);
                    BinaryPrimitives.WriteSingleLittleEndian(variationBytes.Slice(offset + 4, 4), variations[i].Value);
                }

                variationsPtr = _context.Malloc(byteCount);
                if (variationsPtr == 0)
                {
                    throw new OutOfMemoryException("Failed to allocate WASM memory.");
                }
                _context.WriteBytes(variationsPtr, variationBytes);
            }

            var glyphBufferHandle = _context.ShapeFull(
                _handle,
                (int)bufferHandle,
                featuresPtr,
                features.Length,
                variationsPtr,
                variations.Length
            );

            if (glyphBufferHandle == 0)
            {
                throw new InvalidOperationException("Shaping failed.");
            }

            return new WasmGlyphBuffer(_context, glyphBufferHandle);
        }
        finally
        {
            if (featuresPtr != 0) _context.Free(featuresPtr, features.Length * 16);
            if (variationsPtr != 0) _context.Free(variationsPtr, variations.Length * 8);
            if (rentedFeatures != null) ArrayPool<byte>.Shared.Return(rentedFeatures);
            if (rentedVariations != null) ArrayPool<byte>.Shared.Return(rentedVariations);
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
