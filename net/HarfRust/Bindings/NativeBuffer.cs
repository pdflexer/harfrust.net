using System.Runtime.InteropServices;
using System.Text;
using HarfRust.Bindings;

namespace HarfRust.Bindings;

/// <summary>
/// FFI implementation of backend buffer.
/// </summary>
internal sealed unsafe class NativeBuffer : IBackendBuffer
{
    private Bindings.HarfRustBuffer* _handle;
    private bool _disposed;
    private bool _consumed;

    public NativeBuffer()
    {
        _handle = NativeMethods.harfrust_buffer_new();
        if (_handle == null)
        {
            throw new InvalidOperationException("Failed to create HarfRustBuffer");
        }
    }

    internal NativeBuffer(Bindings.HarfRustBuffer* handle)
    {
        _handle = handle;
    }

    public int Length
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return NativeMethods.harfrust_buffer_len(_handle);
        }
    }

    public void AddString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfDisposedOrConsumed();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        fixed (char* ptr = text)
        {
            var result = NativeMethods.harfrust_buffer_add_utf16(_handle, (ushort*)ptr, text.Length);
            if (result != 0)
            {
                throw new InvalidOperationException($"Failed to add string to buffer (error code: {result})");
            }
        }
    }

    public void Clear()
    {
        ThrowIfDisposedOrConsumed();
        NativeMethods.harfrust_buffer_clear(_handle);
    }

    public Direction Direction
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return (Direction)NativeMethods.harfrust_buffer_get_direction(_handle);
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            NativeMethods.harfrust_buffer_set_direction(_handle, (Bindings.HarfRustDirection)value);
        }
    }

    public uint Script
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return NativeMethods.harfrust_buffer_get_script(_handle);
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            NativeMethods.harfrust_buffer_set_script(_handle, value);
        }
    }

    public void SetLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        ThrowIfDisposedOrConsumed();

        var bytes = Encoding.UTF8.GetBytes(language + '\0');
        fixed (byte* ptr = bytes)
        {
            var result = NativeMethods.harfrust_buffer_set_language(_handle, ptr);
            if (result != 0)
            {
                throw new ArgumentException($"Invalid language tag: {language} (error code: {result})", nameof(language));
            }
        }
    }

    public void GuessSegmentProperties()
    {
        ThrowIfDisposedOrConsumed();
        NativeMethods.harfrust_buffer_guess_segment_properties(_handle);
    }

    public nint ConsumeHandle()
    {
        ThrowIfDisposedOrConsumed();
        _consumed = true;
        var handle = _handle;
        _handle = null;
        return (nint)handle;
    }

    private void ThrowIfDisposedOrConsumed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeBuffer));
        }
        if (_consumed)
        {
            throw new InvalidOperationException("Buffer has been consumed by a shaping operation.");
        }
    }

    public void Dispose()
    {
        if (!_disposed && !_consumed)
        {
            if (_handle != null)
            {
                NativeMethods.harfrust_buffer_free(_handle);
                _handle = null;
            }
        }
        _disposed = true;
    }
}
