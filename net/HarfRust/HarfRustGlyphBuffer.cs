using System.Runtime.InteropServices;
using HarfRust.Bindings;

namespace HarfRust;

/// <summary>
/// Glyph information after text shaping.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct GlyphInfo
{
    /// <summary>
    /// The glyph ID in the font.
    /// </summary>
    public readonly uint GlyphId;

    /// <summary>
    /// The cluster index (position in original text).
    /// </summary>
    public readonly uint Cluster;

    internal GlyphInfo(uint glyphId, uint cluster)
    {
        GlyphId = glyphId;
        Cluster = cluster;
    }
}

/// <summary>
/// Glyph positioning information after text shaping.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct GlyphPosition
{
    /// <summary>
    /// Horizontal advance after drawing this glyph.
    /// </summary>
    public readonly int XAdvance;

    /// <summary>
    /// Vertical advance after drawing this glyph.
    /// </summary>
    public readonly int YAdvance;

    /// <summary>
    /// Horizontal offset for drawing.
    /// </summary>
    public readonly int XOffset;

    /// <summary>
    /// Vertical offset for drawing.
    /// </summary>
    public readonly int YOffset;

    internal GlyphPosition(int xAdvance, int yAdvance, int xOffset, int yOffset)
    {
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}

/// <summary>
/// The result of text shaping, containing glyph information and positions.
/// </summary>
public sealed unsafe class HarfRustGlyphBuffer : IDisposable
{
    private Bindings.HarfRustGlyphBuffer* _handle;
    private bool _disposed;

    internal HarfRustGlyphBuffer(Bindings.HarfRustGlyphBuffer* handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets the number of glyphs in the buffer.
    /// </summary>
    public int Length
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.harfrust_glyph_buffer_len(_handle);
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
            var ptr = NativeMethods.harfrust_glyph_buffer_get_infos(_handle);
            var len = NativeMethods.harfrust_glyph_buffer_len(_handle);
            if (ptr == null || len <= 0)
            {
                return ReadOnlySpan<GlyphInfo>.Empty;
            }
            return new ReadOnlySpan<GlyphInfo>(ptr, len);
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
            var ptr = NativeMethods.harfrust_glyph_buffer_get_positions(_handle);
            var len = NativeMethods.harfrust_glyph_buffer_len(_handle);
            if (ptr == null || len <= 0)
            {
                return ReadOnlySpan<GlyphPosition>.Empty;
            }
            return new ReadOnlySpan<GlyphPosition>(ptr, len);
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
        
        var bufferHandle = NativeMethods.harfrust_glyph_buffer_into_buffer(_handle);
        _handle = null;
        _disposed = true;

        if (bufferHandle == null)
        {
            throw new InvalidOperationException("Failed to convert glyph buffer back to unicode buffer.");
        }

        return new HarfRustBuffer(bufferHandle);
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
            if (_handle != null)
            {
                NativeMethods.harfrust_glyph_buffer_free(_handle);
                _handle = null;
            }
            _disposed = true;
        }
    }
}
