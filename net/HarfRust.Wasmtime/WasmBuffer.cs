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
        ThrowIfDisposedOrConsumed();

        if (string.IsNullOrEmpty(text))
            return;

        // Convert to UTF-16 bytes
        var bytes = Encoding.Unicode.GetBytes(text);
        var ptr = _context.AllocateAndWrite(bytes);
        try
        {
            var result = 0; // harfrust_buffer_add_utf16 returns int, called via action
            _context.BufferAddUtf16(_handle, ptr, text.Length);
        }
        finally
        {
            _context.Free(ptr);
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
        ThrowIfDisposedOrConsumed();

        var bytes = Encoding.UTF8.GetBytes(language + '\0');
        var ptr = _context.AllocateAndWrite(bytes);
        try
        {
            _context.BufferSetLanguage(_handle, ptr);
        }
        finally
        {
            _context.Free(ptr);
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
