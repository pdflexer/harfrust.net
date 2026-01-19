namespace HarfRust;

/// <summary>
/// Backend provider for HarfRust text shaping.
/// </summary>
public interface IHarfRustBackend
{
    /// <summary>
    /// Creates a new buffer for accumulating text to shape.
    /// </summary>
    IBackendBuffer CreateBuffer();

    /// <summary>
    /// Creates a font from raw font data (TTF/OTF bytes).
    /// </summary>
    IBackendFont CreateFont(byte[] data);

    /// <summary>
    /// Creates a font from raw font data at a specific index (for font collections).
    /// </summary>
    IBackendFont CreateFont(byte[] data, uint index);
}

/// <summary>
/// Backend buffer for accumulating text to be shaped.
/// </summary>
public interface IBackendBuffer : IDisposable
{
    /// <summary>
    /// Gets the number of characters currently in the buffer.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Adds a string to the buffer for shaping.
    /// </summary>
    void AddString(string text);

    /// <summary>
    /// Clears all content from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets or sets the text direction.
    /// </summary>
    Direction Direction { get; set; }

    /// <summary>
    /// Gets or sets the script as an ISO 15924 tag.
    /// </summary>
    uint Script { get; set; }

    /// <summary>
    /// Sets the language from a BCP 47 language tag.
    /// </summary>
    void SetLanguage(string language);

    /// <summary>
    /// Guesses and sets the segment properties based on buffer contents.
    /// </summary>
    void GuessSegmentProperties();

    /// <summary>
    /// Consumes the buffer handle for use in shaping.
    /// After calling this, the buffer is no longer usable by the caller.
    /// </summary>
    /// <returns>An opaque handle representing the consumed buffer.</returns>
    nint ConsumeHandle();
}

/// <summary>
/// Backend font for text shaping.
/// </summary>
public interface IBackendFont : IDisposable
{
    /// <summary>
    /// Gets the font's units per em value.
    /// </summary>
    int UnitsPerEm { get; }

    /// <summary>
    /// Shapes the text in the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to shape. The buffer is consumed and should not be used after shaping.</param>
    IBackendGlyphBuffer Shape(IBackendBuffer buffer);

    /// <summary>
    /// Shapes the text in the buffer with features and variations.
    /// </summary>
    IBackendGlyphBuffer Shape(IBackendBuffer buffer, Feature[]? features, Variation[]? variations);
}

/// <summary>
/// Backend glyph buffer containing shaping results.
/// </summary>
public interface IBackendGlyphBuffer : IDisposable
{
    /// <summary>
    /// Gets the number of glyphs in the buffer.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the glyph information.
    /// </summary>
    ReadOnlySpan<GlyphInfo> GlyphInfos { get; }

    /// <summary>
    /// Gets the glyph positions.
    /// </summary>
    ReadOnlySpan<GlyphPosition> GlyphPositions { get; }

    /// <summary>
    /// Converts back to a reusable backend buffer.
    /// </summary>
    IBackendBuffer IntoBuffer();
}
