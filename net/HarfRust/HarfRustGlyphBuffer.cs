namespace HarfRust;

/// <summary>
/// The result of text shaping, containing glyph information and positions.
/// </summary>
public sealed class HarfRustGlyphBuffer : IDisposable
{
    private readonly IBackendGlyphBuffer _backend;
    private readonly Action<IBackendBuffer>? _bufferReturn;
    private bool _disposed;

    internal HarfRustGlyphBuffer(IBackendGlyphBuffer backend)
        : this(backend, null)
    {
    }

    internal HarfRustGlyphBuffer(IBackendGlyphBuffer backend, Action<IBackendBuffer>? bufferReturn)
    {
        _backend = backend;
        _bufferReturn = bufferReturn;
    }

    /// <summary>
    /// Gets the number of glyphs in the buffer.
    /// </summary>
    public int Length
    {
        get
        {
            ThrowIfDisposed();
            return _backend.Length;
        }
    }

    /// <summary>
    /// Gets a read-only span of glyph information.
    /// </summary>
    /// <remarks>
    /// The span is only valid while the glyph buffer is not disposed.
    /// </remarks>
    public ReadOnlySpan<GlyphInfo> GlyphInfos
    {
        get
        {
            ThrowIfDisposed();
            return _backend.GlyphInfos;
        }
    }

    /// <summary>
    /// Gets a read-only span of glyph positions.
    /// </summary>
    /// <remarks>
    /// The span is only valid while the glyph buffer is not disposed.
    /// </remarks>
    public ReadOnlySpan<GlyphPosition> GlyphPositions
    {
        get
        {
            ThrowIfDisposed();
            return _backend.GlyphPositions;
        }
    }

    /// <summary>
    /// Clears the glyph buffer and returns a unicode buffer for reuse.
    /// </summary>
    /// <returns>A unicode buffer that can be reused for another shaping operation.</returns>
    /// <remarks>
    /// This consumes the glyph buffer. After calling this method, the glyph buffer
    /// is disposed and cannot be used again.
    /// </remarks>
    public HarfRustBuffer IntoBuffer()
    {
        ThrowIfDisposed();
        if (_bufferReturn != null)
        {
            throw new InvalidOperationException("This glyph buffer is owned by a shape session and cannot be converted into a standalone buffer.");
        }

        var buffer = _backend.IntoBuffer();
        _disposed = true;

        return new HarfRustBuffer(buffer);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HarfRustGlyphBuffer));
        }
    }

    /// <summary>
    /// Releases all native resources associated with this glyph buffer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_bufferReturn == null)
            {
                _backend.Dispose();
            }
            else
            {
                var buffer = _backend.IntoBuffer();
                _bufferReturn(buffer);
            }
            _disposed = true;
        }
    }
}
