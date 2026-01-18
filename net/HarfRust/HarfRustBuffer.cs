using System.Runtime.InteropServices;
using System.Text;
using HarfRust.Bindings;

namespace HarfRust;

/// <summary>
/// A buffer for accumulating text to be shaped by the harfrust text shaping engine.
/// </summary>
/// <remarks>
/// This class wraps the native harfrust UnicodeBuffer and manages its lifecycle.
/// Always dispose of this object when done to free native resources.
/// </remarks>
public sealed unsafe class HarfRustBuffer : IDisposable
{
    private Bindings.HarfRustBuffer* _handle;
    private bool _disposed;
    private bool _consumed;

    /// <summary>
    /// Creates a new empty text buffer.
    /// </summary>
    public HarfRustBuffer()
    {
        _handle = NativeMethods.harfrust_buffer_new();
        if (_handle == null)
        {
            throw new InvalidOperationException("Failed to create HarfRustBuffer");
        }
    }

    /// <summary>
    /// Internal constructor for creating from a native handle.
    /// </summary>
    internal HarfRustBuffer(Bindings.HarfRustBuffer* handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Helper to create a 4-byte script tag from a string (e.g., "Latn").
    /// </summary>
    public static uint CreateScriptTag(string tag)
    {
        if (tag == null || tag.Length != 4)
        {
            throw new ArgumentException("Tag must be exactly 4 characters.", nameof(tag));
        }
            
        return ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | (uint)tag[3];
    }

    /// <summary>
    /// Gets the number of characters currently in the buffer.
    /// </summary>
    public int Length
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return NativeMethods.harfrust_buffer_len(_handle);
        }
    }

    /// <summary>
    /// Adds a string to the buffer for shaping.
    /// </summary>
    /// <param name="text">The text to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if text is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
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

    /// <summary>
    /// Clears all content from the buffer, preparing it for reuse.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public void Clear()
    {
        ThrowIfDisposedOrConsumed();
        NativeMethods.harfrust_buffer_clear(_handle);
    }

    /// <summary>
    /// Consumes the buffer handle for use in shaping.
    /// After calling this, the buffer is no longer usable.
    /// </summary>
    internal Bindings.HarfRustBuffer* ConsumeHandle()
    {
        ThrowIfDisposedOrConsumed();
        _consumed = true;
        var handle = _handle;
        _handle = null;
        return handle;
    }

    private void ThrowIfDisposedOrConsumed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HarfRustBuffer));
        }
        if (_consumed)
        {
            throw new InvalidOperationException("Buffer has been consumed by a shaping operation.");
        }
    }

    /// <summary>
    /// Gets or sets the text direction of the buffer.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the script of the buffer as an ISO 15924 tag.
    /// </summary>
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

    /// <summary>
    /// Sets the language of the buffer from a BCP 47 language tag string.
    /// </summary>
    /// <param name="language">The language tag (e.g., "en", "zh-Hans").</param>
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

    /// <summary>
    /// Guesses and sets the segment properties (direction, script, language) based on the buffer contents.
    /// </summary>
    public void GuessSegmentProperties()
    {
        ThrowIfDisposedOrConsumed();
        NativeMethods.harfrust_buffer_guess_segment_properties(_handle);
    }

    /// <summary>
    /// Releases all native resources associated with this buffer.
    /// </summary>
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
