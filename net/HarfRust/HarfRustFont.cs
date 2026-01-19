namespace HarfRust;

/// <summary>
/// Represents a loaded font for text shaping.
/// </summary>
/// <remarks>
/// The font owns a copy of the font data and keeps it alive for the lifetime of the font.
/// Use <see cref="Shape"/> to perform text shaping with this font.
/// </remarks>
public sealed class HarfRustFont : IDisposable
{
    private readonly IBackendFont _backend;
    private bool _disposed;

    /// <summary>
    /// Creates a font from raw font data (TTF/OTF bytes).
    /// </summary>
    /// <param name="data">The font file data.</param>
    /// <param name="backend">Optional backend to use. Defaults to FFI backend.</param>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    /// <exception cref="ArgumentException">Thrown if data is empty or invalid.</exception>
    public HarfRustFont(byte[] data, IHarfRustBackend? backend = null)
    {
        backend ??= HarfRustBackend.Current;
        _backend = backend.CreateFont(data);
    }

    /// <summary>
    /// Creates a font from raw font data at a specific index (for font collections like TTC).
    /// </summary>
    /// <param name="data">The font file data.</param>
    /// <param name="index">The font index within the collection (0 for single fonts).</param>
    /// <param name="backend">Optional backend to use. Defaults to FFI backend.</param>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    /// <exception cref="ArgumentException">Thrown if data is empty, invalid, or index is out of range.</exception>
    public HarfRustFont(byte[] data, uint index, IHarfRustBackend? backend = null)
    {
        backend ??= HarfRustBackend.Current;
        _backend = backend.CreateFont(data, index);
    }

    /// <summary>
    /// Creates a font from a file path.
    /// </summary>
    /// <param name="path">The path to the font file.</param>
    /// <param name="backend">Optional backend to use. Defaults to current backend.</param>
    /// <returns>A new font instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the font data is invalid.</exception>
    public static HarfRustFont FromFile(string path, IHarfRustBackend? backend = null)
    {
        var data = File.ReadAllBytes(path);
        return new HarfRustFont(data, backend);
    }

    /// <summary>
    /// Creates a font from a file path at a specific index (for font collections).
    /// </summary>
    /// <param name="path">The path to the font file.</param>
    /// <param name="index">The font index within the collection.</param>
    /// <param name="backend">Optional backend to use. Defaults to current backend.</param>
    /// <returns>A new font instance.</returns>
    public static HarfRustFont FromFile(string path, uint index, IHarfRustBackend? backend = null)
    {
        var data = File.ReadAllBytes(path);
        return new HarfRustFont(data, index, backend);
    }

    /// <summary>
    /// Creates a font from a stream.
    /// </summary>
    /// <param name="stream">The stream containing font data.</param>
    /// <param name="backend">Optional backend to use. Defaults to current backend.</param>
    /// <returns>A new font instance.</returns>
    public static HarfRustFont FromStream(Stream stream, IHarfRustBackend? backend = null)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return new HarfRustFont(memoryStream.ToArray(), backend);
    }

    /// <summary>
    /// Gets the font's units per em value.
    /// </summary>
    public int UnitsPerEm
    {
        get
        {
            ThrowIfDisposed();
            return _backend.UnitsPerEm;
        }
    }

    /// <summary>
    /// Shapes the text in the buffer and returns the shaping results.
    /// </summary>
    /// <param name="buffer">The buffer containing text to shape. This buffer is consumed and cannot be used after calling this method.</param>
    /// <returns>A glyph buffer containing the shaping results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if buffer is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if shaping fails.</exception>
    /// <remarks>
    /// The input buffer is consumed by this operation. To reuse the buffer allocation,
    /// call <see cref="HarfRustGlyphBuffer.IntoBuffer"/> on the returned glyph buffer.
    /// </remarks>
    public HarfRustGlyphBuffer Shape(HarfRustBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfDisposed();

        var result = _backend.Shape(buffer.BackendBuffer);
        return new HarfRustGlyphBuffer(result);
    }

    /// <summary>
    /// Shapes the text in the buffer with specific OpenType features and variable font settings.
    /// </summary>
    /// <param name="buffer">The buffer containing text to shape. This buffer is consumed.</param>
    /// <param name="features">OpenType features to apply.</param>
    /// <param name="variations">Variable font axis settings.</param>
    /// <returns>A glyph buffer containing the shaping results.</returns>
    public HarfRustGlyphBuffer Shape(HarfRustBuffer buffer, Feature[]? features = null, Variation[]? variations = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ThrowIfDisposed();

        if ((features == null || features.Length == 0) && (variations == null || variations.Length == 0))
        {
            return Shape(buffer);
        }

        var result = _backend.Shape(buffer.BackendBuffer, features, variations);
        return new HarfRustGlyphBuffer(result);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HarfRustFont));
        }
    }

    /// <summary>
    /// Releases all native resources associated with this font.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _backend.Dispose();
            _disposed = true;
        }
    }
}
