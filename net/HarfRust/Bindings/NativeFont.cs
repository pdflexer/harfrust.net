using HarfRust.Bindings;

namespace HarfRust.Bindings;

/// <summary>
/// FFI implementation of backend font.
/// </summary>
internal sealed unsafe class NativeFont : IBackendFont
{
    private Bindings.HarfRustFont* _handle;
    private bool _disposed;

    public NativeFont(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Font data cannot be empty.", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            _handle = NativeMethods.harfrust_font_from_data(dataPtr, data.Length);
        }

        if (_handle == null)
        {
            throw new ArgumentException("Invalid font data.", nameof(data));
        }
    }

    public NativeFont(byte[] data, uint index)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Font data cannot be empty.", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            _handle = NativeMethods.harfrust_font_from_data_index(dataPtr, data.Length, index);
        }

        if (_handle == null)
        {
            throw new ArgumentException("Invalid font data or index out of range.", nameof(data));
        }
    }

    public int UnitsPerEm
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.harfrust_font_units_per_em(_handle);
        }
    }

    public IBackendGlyphBuffer Shape(IBackendBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfDisposed();

        var bufferHandle = buffer.ConsumeHandle();
        var glyphBuffer = NativeMethods.harfrust_shape(_handle, (Bindings.HarfRustBuffer*)bufferHandle);
        if (glyphBuffer == null)
        {
            throw new InvalidOperationException("Shaping failed.");
        }

        return new NativeGlyphBuffer(glyphBuffer);
    }

    public IBackendGlyphBuffer Shape(IBackendBuffer buffer, Feature[]? features, Variation[]? variations)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfDisposed();

        if ((features == null || features.Length == 0) && (variations == null || variations.Length == 0))
        {
            return Shape(buffer);
        }

        var bufferHandle = buffer.ConsumeHandle();

        // Map features
        Bindings.HarfRustFeature[]? nativeFeatures = null;
        if (features != null && features.Length > 0)
        {
            nativeFeatures = new Bindings.HarfRustFeature[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                nativeFeatures[i] = new Bindings.HarfRustFeature
                {
                    tag = features[i].Tag,
                    value = features[i].Value,
                    start = features[i].Start,
                    end = features[i].End
                };
            }
        }

        // Map variations
        Bindings.HarfRustVariation[]? nativeVariations = null;
        if (variations != null && variations.Length > 0)
        {
            nativeVariations = new Bindings.HarfRustVariation[variations.Length];
            for (int i = 0; i < variations.Length; i++)
            {
                nativeVariations[i] = new Bindings.HarfRustVariation
                {
                    tag = variations[i].Tag,
                    value = variations[i].Value
                };
            }
        }

        Bindings.HarfRustGlyphBuffer* glyphBuffer;

        fixed (Bindings.HarfRustFeature* featPtr = nativeFeatures)
        fixed (Bindings.HarfRustVariation* varPtr = nativeVariations)
        {
            glyphBuffer = NativeMethods.harfrust_shape_full(
                _handle,
                (Bindings.HarfRustBuffer*)bufferHandle,
                featPtr,
                nativeFeatures == null ? 0 : (uint)nativeFeatures.Length,
                varPtr,
                nativeVariations == null ? 0 : (uint)nativeVariations.Length
            );
        }

        if (glyphBuffer == null)
        {
            throw new InvalidOperationException("Shaping failed.");
        }

        return new NativeGlyphBuffer(glyphBuffer);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeFont));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != null)
            {
                NativeMethods.harfrust_font_free(_handle);
                _handle = null;
            }
            _disposed = true;
        }
    }
}
