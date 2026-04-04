namespace HarfRust;

/// <summary>
/// Reuses a shaping buffer across multiple operations.
/// </summary>
public sealed class HarfRustShapeSession : IDisposable
{
    private IBackendBuffer? _buffer;
    private bool _disposed;
    private bool _shapeInProgress;

    /// <summary>
    /// Creates a new shape session using the current backend.
    /// </summary>
    public HarfRustShapeSession() : this(HarfRustBackend.Current)
    {
    }

    /// <summary>
    /// Creates a new shape session using the specified backend.
    /// </summary>
    public HarfRustShapeSession(IHarfRustBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _buffer = backend.CreateBuffer();
    }

    /// <summary>
    /// Gets the current text length in the session buffer.
    /// </summary>
    public int Length
    {
        get
        {
            return GetBuffer().Length;
        }
    }

    /// <summary>
    /// Gets or sets the text direction of the buffer.
    /// </summary>
    public Direction Direction
    {
        get => GetBuffer().Direction;
        set => GetBuffer().Direction = value;
    }

    /// <summary>
    /// Gets or sets the script of the buffer as an ISO 15924 tag.
    /// </summary>
    public uint Script
    {
        get => GetBuffer().Script;
        set => GetBuffer().Script = value;
    }

    /// <summary>
    /// Adds text to the buffer.
    /// </summary>
    public void Add(ReadOnlySpan<char> text)
    {
        GetBuffer().Add(text);
    }

    /// <summary>
    /// Adds text to the buffer.
    /// </summary>
    public void Add(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Add(text.AsSpan());
    }

    /// <summary>
    /// Clears the buffer for reuse.
    /// </summary>
    public void Clear()
    {
        GetBuffer().Clear();
    }

    /// <summary>
    /// Sets the language from a BCP 47 language tag.
    /// </summary>
    public void SetLanguage(ReadOnlySpan<char> language)
    {
        GetBuffer().SetLanguage(language);
    }

    /// <summary>
    /// Sets the language from a BCP 47 language tag.
    /// </summary>
    public void SetLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        SetLanguage(language.AsSpan());
    }

    /// <summary>
    /// Guesses segment properties from the current buffer contents.
    /// </summary>
    public void GuessSegmentProperties()
    {
        GetBuffer().GuessSegmentProperties();
    }

    /// <summary>
    /// Shapes the current buffer contents.
    /// </summary>
    public HarfRustGlyphBuffer Shape(HarfRustFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        return Shape(font, ReadOnlySpan<Feature>.Empty, ReadOnlySpan<Variation>.Empty);
    }

    /// <summary>
    /// Shapes the current buffer contents with features and variations.
    /// </summary>
    public HarfRustGlyphBuffer Shape(HarfRustFont font, ReadOnlySpan<Feature> features, ReadOnlySpan<Variation> variations = default)
    {
        ArgumentNullException.ThrowIfNull(font);
        ThrowIfDisposed();
        EnsureNoInFlightShape();

        var buffer = _buffer ?? throw new InvalidOperationException("Shape session does not have an available buffer.");
        _buffer = null;
        _shapeInProgress = true;

        try
        {
            var result = font.BackendFont.Shape(buffer, features, variations);
            return new HarfRustGlyphBuffer(result, ReturnBuffer);
        }
        catch
        {
            _buffer = buffer;
            _shapeInProgress = false;
            throw;
        }
    }

    /// <summary>
    /// Clears the current buffer, adds the supplied text, optionally guesses segment properties, and shapes it.
    /// </summary>
    public HarfRustGlyphBuffer Shape(HarfRustFont font, ReadOnlySpan<char> text, ReadOnlySpan<Feature> features = default, ReadOnlySpan<Variation> variations = default, bool guessSegmentProperties = true)
    {
        Clear();
        Add(text);
        if (guessSegmentProperties)
        {
            GuessSegmentProperties();
        }

        return Shape(font, features, variations);
    }

    /// <summary>
    /// Clears the current buffer, adds the supplied text, optionally guesses segment properties, and shapes it.
    /// </summary>
    public HarfRustGlyphBuffer Shape(HarfRustFont font, string text, ReadOnlySpan<Feature> features = default, ReadOnlySpan<Variation> variations = default, bool guessSegmentProperties = true)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Shape(font, text.AsSpan(), features, variations, guessSegmentProperties);
    }

    private IBackendBuffer GetBuffer()
    {
        ThrowIfDisposed();
        EnsureNoInFlightShape();
        return _buffer ?? throw new InvalidOperationException("Shape session does not have an available buffer.");
    }

    private void EnsureNoInFlightShape()
    {
        if (_shapeInProgress)
        {
            throw new InvalidOperationException("The shape session already has an active glyph buffer. Dispose the current glyph buffer before reusing the session.");
        }
    }

    private void ReturnBuffer(IBackendBuffer buffer)
    {
        if (_disposed)
        {
            buffer.Dispose();
        }
        else
        {
            _buffer = buffer;
            _shapeInProgress = false;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HarfRustShapeSession));
        }
    }

    /// <summary>
    /// Releases all resources associated with the session.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buffer?.Dispose();
        _buffer = null;
        _disposed = true;
    }
}
