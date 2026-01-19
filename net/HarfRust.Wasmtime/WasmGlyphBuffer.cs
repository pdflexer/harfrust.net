using System.Runtime.InteropServices;

namespace HarfRust.Wasmtime;

/// <summary>
/// Wasmtime-based glyph buffer implementation.
/// </summary>
internal sealed class WasmGlyphBuffer : IBackendGlyphBuffer
{
    private readonly WasmContext _context;
    private int _handle;
    private bool _disposed;

    // Cached data read from WASM memory
    private GlyphInfo[]? _glyphInfos;
    private GlyphPosition[]? _glyphPositions;

    internal WasmGlyphBuffer(WasmContext context, int handle)
    {
        _context = context;
        _handle = handle;
    }

    public int Length
    {
        get
        {
            ThrowIfDisposed();
            return _context.GlyphBufferLen(_handle);
        }
    }

    public ReadOnlySpan<GlyphInfo> GlyphInfos
    {
        get
        {
            ThrowIfDisposed();
            EnsureCached();
            return _glyphInfos!;
        }
    }

    public ReadOnlySpan<GlyphPosition> GlyphPositions
    {
        get
        {
            ThrowIfDisposed();
            EnsureCached();
            return _glyphPositions!;
        }
    }

    private void EnsureCached()
    {
        if (_glyphInfos != null)
            return;

        var len = _context.GlyphBufferLen(_handle);
        if (len <= 0)
        {
            _glyphInfos = Array.Empty<GlyphInfo>();
            _glyphPositions = Array.Empty<GlyphPosition>();
            return;
        }

        // Read glyph infos from WASM memory
        var infosPtr = _context.GlyphBufferGetInfos(_handle);
        var infosBytes = _context.ReadBytes(infosPtr, len * 8); // 2 uints = 8 bytes per info
        _glyphInfos = new GlyphInfo[len];
        for (int i = 0; i < len; i++)
        {
            var offset = i * 8;
            var glyphId = MemoryMarshal.Read<uint>(infosBytes.Slice(offset, 4));
            var cluster = MemoryMarshal.Read<uint>(infosBytes.Slice(offset + 4, 4));
            _glyphInfos[i] = new GlyphInfo(glyphId, cluster);
        }

        // Read glyph positions from WASM memory
        var positionsPtr = _context.GlyphBufferGetPositions(_handle);
        var positionsBytes = _context.ReadBytes(positionsPtr, len * 16); // 4 ints = 16 bytes per position
        _glyphPositions = new GlyphPosition[len];
        for (int i = 0; i < len; i++)
        {
            var offset = i * 16;
            var xAdvance = MemoryMarshal.Read<int>(positionsBytes.Slice(offset, 4));
            var yAdvance = MemoryMarshal.Read<int>(positionsBytes.Slice(offset + 4, 4));
            var xOffset = MemoryMarshal.Read<int>(positionsBytes.Slice(offset + 8, 4));
            var yOffset = MemoryMarshal.Read<int>(positionsBytes.Slice(offset + 12, 4));
            _glyphPositions[i] = new GlyphPosition(xAdvance, yAdvance, xOffset, yOffset);
        }
    }

    public IBackendBuffer IntoBuffer()
    {
        ThrowIfDisposed();

        var bufferHandle = _context.GlyphBufferIntoBuffer(_handle);
        _handle = 0;
        _disposed = true;

        if (bufferHandle == 0)
        {
            throw new InvalidOperationException("Failed to convert glyph buffer back to buffer.");
        }

        return new WasmBuffer(_context, bufferHandle);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmGlyphBuffer));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_handle != 0)
            {
                _context.GlyphBufferFree(_handle);
                _handle = 0;
            }
            // Don't dispose context here - it may be shared or owned by font
            _disposed = true;
        }
    }
}
