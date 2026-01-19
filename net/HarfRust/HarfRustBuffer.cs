namespace HarfRust;

/// <summary>
/// A buffer for accumulating text to be shaped by the harfrust text shaping engine.
/// </summary>
/// <remarks>
/// This class wraps the native harfrust UnicodeBuffer and manages its lifecycle.
/// Always dispose of this object when done to free native resources.
/// </remarks>
public sealed class HarfRustBuffer : IDisposable
{
    private readonly IBackendBuffer _backend;
    private bool _disposed;
    private bool _consumed;

    /// <summary>
    /// Creates a new empty text buffer using the current default backend.
    /// </summary>
    public HarfRustBuffer() : this(HarfRustBackend.Current)
    {
    }

    /// <summary>
    /// Creates a new empty text buffer using the specified backend.
    /// </summary>
    /// <param name="backend">The backend to use for buffer operations.</param>
    public HarfRustBuffer(IHarfRustBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _backend = backend.CreateBuffer();
    }

    /// <summary>
    /// Internal constructor for creating from a backend buffer.
    /// </summary>
    internal HarfRustBuffer(IBackendBuffer backend)
    {
        _backend = backend;
    }

    internal IBackendBuffer BackendBuffer => _backend;

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
            return _backend.Length;
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
        _backend.AddString(text);
    }

    /// <summary>
    /// Clears all content from the buffer, preparing it for reuse.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the buffer has been disposed.</exception>
    public void Clear()
    {
        ThrowIfDisposedOrConsumed();
        _backend.Clear();
    }

    /// <summary>
    /// Gets or sets the text direction of the buffer.
    /// </summary>
    public Direction Direction
    {
        get
        {
            ThrowIfDisposedOrConsumed();
            return _backend.Direction;
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            _backend.Direction = value;
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
            return _backend.Script;
        }
        set
        {
            ThrowIfDisposedOrConsumed();
            _backend.Script = value;
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
        _backend.SetLanguage(language);
    }

    /// <summary>
    /// Guesses and sets the segment properties (direction, script, language) based on the buffer contents.
    /// </summary>
    public void GuessSegmentProperties()
    {
        ThrowIfDisposedOrConsumed();
        _backend.GuessSegmentProperties();
    }

    /// <summary>
    /// Consumes the buffer handle for use in shaping.
    /// After calling this, the buffer is no longer usable.
    /// </summary>
    internal nint ConsumeHandle()
    {
        ThrowIfDisposedOrConsumed();
        _consumed = true;
        return _backend.ConsumeHandle();
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
    /// Releases all native resources associated with this buffer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed && !_consumed)
        {
            _backend.Dispose();
        }
        _disposed = true;
    }
}
