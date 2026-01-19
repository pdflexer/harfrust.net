using HarfRust.Bindings;

namespace HarfRust.Bindings;

/// <summary>
/// FFI implementation of backend glyph buffer.
/// </summary>
internal sealed unsafe class NativeGlyphBuffer : IBackendGlyphBuffer
{
    private Bindings.HarfRustGlyphBuffer* _handle;
    private bool _disposed;

    internal NativeGlyphBuffer(Bindings.HarfRustGlyphBuffer* handle)
    {
        _handle = handle;
    }

    public int Length
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.harfrust_glyph_buffer_len(_handle);
        }
    }

    public ReadOnlySpan<GlyphInfo> GlyphInfos
    {
        get
        {
            ThrowIfDisposed();
            var ptr = NativeMethods.harfrust_glyph_buffer_get_infos(_handle);
            var len = NativeMethods.harfrust_glyph_buffer_len(_handle);
            if (ptr == null || len <= 0)
            {
                return ReadOnlySpan<GlyphInfo>.Empty;
            }
            return new ReadOnlySpan<GlyphInfo>(ptr, len);
        }
    }

    public ReadOnlySpan<GlyphPosition> GlyphPositions
    {
        get
        {
            ThrowIfDisposed();
            var ptr = NativeMethods.harfrust_glyph_buffer_get_positions(_handle);
            var len = NativeMethods.harfrust_glyph_buffer_len(_handle);
            if (ptr == null || len <= 0)
            {
                return ReadOnlySpan<GlyphPosition>.Empty;
            }
            return new ReadOnlySpan<GlyphPosition>(ptr, len);
        }
    }

    public IBackendBuffer IntoBuffer()
    {
        ThrowIfDisposed();

        var bufferHandle = NativeMethods.harfrust_glyph_buffer_into_buffer(_handle);
        _handle = null;
        _disposed = true;

        if (bufferHandle == null)
        {
            throw new InvalidOperationException("Failed to convert glyph buffer back to unicode buffer.");
        }

        return new NativeBuffer(bufferHandle);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeGlyphBuffer));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != null)
            {
                NativeMethods.harfrust_glyph_buffer_free(_handle);
                _handle = null;
            }
            _disposed = true;
        }
    }
}
