using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace HarfRust.Wasmtime;

/// <summary>
/// Wasmtime-based buffer implementation.
/// </summary>
internal sealed class WasmBuffer : IBackendBuffer
{
    private readonly WasmContext _context;
    private int _handle;
    private bool _disposed;
    private bool _consumed;

    public WasmBuffer(WasmContext context)
    {
        _context = context;
        _handle = _context.BufferNew();
        if (_handle == 0)
        {
            throw new InvalidOperationException("Failed to create buffer.");
        }
    }

    internal WasmBuffer(WasmContext context, int handle)
    {
        _context = context;
        _handle = handle;
    }

    internal WasmContext Context => _context;

    public int Length
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return _context.BufferLen(_handle);
        }
    }

    public void AddString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Add(text.AsSpan());
    }

    public void Add(ReadOnlySpan<char> text)
    {
        ThrowIfDisposedOrConsumed();

        if (text.IsEmpty)
            return;

        var byteCount = checked(text.Length * sizeof(char));
        var ptr = _context.Malloc(byteCount);
        if (ptr == 0)
        {
            throw new OutOfMemoryException("Failed to allocate WASM memory.");
        }

        try
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("Big-endian platforms are not supported by the Wasmtime backend.");
            }

            _context.WriteBytes(ptr, MemoryMarshal.AsBytes(text));
            var result = _context.BufferAddUtf16(_handle, ptr, text.Length);
            if (result != 0)
            {
                throw new InvalidOperationException($"Failed to add string to buffer (error code: {result})");
            }
        }
        finally
        {
            _context.Free(ptr, byteCount);
        }
    }

    public void Clear()
    {
        ThrowIfDisposedOrConsumed();
        _context.BufferClear(_handle);
    }

    public Direction Direction
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return (Direction)_context.BufferGetDirection(_handle);
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            _context.BufferSetDirection(_handle, (int)value);
        }
    }

    public uint Script
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return (uint)_context.BufferGetScript(_handle);
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            _context.BufferSetScript(_handle, (int)value);
        }
    }

    public void SetLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        SetLanguage(language.AsSpan());
    }

    public void SetLanguage(ReadOnlySpan<char> language)
    {
        ThrowIfDisposedOrConsumed();

        var byteCount = Encoding.UTF8.GetByteCount(language) + 1;
        byte[]? rented = null;
        Span<byte> bytes = byteCount <= 256
            ? stackalloc byte[byteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

        var written = Encoding.UTF8.GetBytes(language, bytes);
        bytes[written] = 0;

        var ptr = _context.Malloc(byteCount);
        if (ptr == 0)
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
            throw new OutOfMemoryException("Failed to allocate WASM memory.");
        }

        try
        {
            _context.WriteBytes(ptr, bytes);
            _context.BufferSetLanguage(_handle, ptr);
        }
        finally
        {
            _context.Free(ptr, byteCount);
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public void GuessSegmentProperties()
    {
        ThrowIfDisposedOrConsumed();
        _context.BufferGuessSegmentProperties(_handle);
    }

    public nint ConsumeHandle()
    {
        ThrowIfDisposedOrConsumed();
        _consumed = true;
        var handle = _handle;
        _handle = 0;
        // Return a combined pointer that includes context reference
        // For now, just return the handle - the font will need access to the context
        return handle;
    }

    private void ThrowIfDisposedOrConsumed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasmBuffer));
        if (_consumed)
            throw new InvalidOperationException("Buffer has been consumed.");
    }

    public void Dispose()
    {
        if (!_disposed && !_consumed)
        {
            if (_handle != 0)
            {
                _context.BufferFree(_handle);
                _handle = 0;
            }

            // Do NOT dispose _context as it is owned by the backend
        }
        _disposed = true;
    }
}
