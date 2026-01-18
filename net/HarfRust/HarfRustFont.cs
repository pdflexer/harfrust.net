using HarfRust.Bindings;

namespace HarfRust;

/// <summary>
/// Represents a loaded font for text shaping.
/// </summary>
/// <remarks>
/// The font owns a copy of the font data and keeps it alive for the lifetime of the font.
/// Use <see cref="Shape"/> to perform text shaping with this font.
/// </remarks>
public sealed unsafe class HarfRustFont : IDisposable
{
    private Bindings.HarfRustFont* _handle;
    private bool _disposed;

    /// <summary>
    /// Creates a font from raw font data (TTF/OTF bytes).
    /// </summary>
    /// <param name="data">The font file data.</param>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    /// <exception cref="ArgumentException">Thrown if data is empty or invalid.</exception>
    public HarfRustFont(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Font data cannot be empty.", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            _handle = NativeMethods.harfrust_font_from_data(dataPtr, data.Length);
        }

        if (_handle == null)
        {
            throw new ArgumentException("Invalid font data.", nameof(data));
        }
    }

    /// <summary>
    /// Creates a font from raw font data at a specific index (for font collections like TTC).
    /// </summary>
    /// <param name="data">The font file data.</param>
    /// <param name="index">The font index within the collection (0 for single fonts).</param>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    /// <exception cref="ArgumentException">Thrown if data is empty, invalid, or index is out of range.</exception>
    public HarfRustFont(byte[] data, uint index)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("Font data cannot be empty.", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            _handle = NativeMethods.harfrust_font_from_data_index(dataPtr, data.Length, index);
        }

        if (_handle == null)
        {
            throw new ArgumentException("Invalid font data or index out of range.", nameof(data));
        }
    }

    /// <summary>
    /// Creates a font from a file path.
    /// </summary>
    /// <param name="path">The path to the font file.</param>
    /// <returns>A new font instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the font data is invalid.</exception>
    public static HarfRustFont FromFile(string path)
    {
        var data = File.ReadAllBytes(path);
        return new HarfRustFont(data);
    }

    /// <summary>
    /// Creates a font from a file path at a specific index (for font collections).
    /// </summary>
    /// <param name="path">The path to the font file.</param>
    /// <param name="index">The font index within the collection.</param>
    /// <returns>A new font instance.</returns>
    public static HarfRustFont FromFile(string path, uint index)
    {
        var data = File.ReadAllBytes(path);
        return new HarfRustFont(data, index);
    }

    /// <summary>
    /// Creates a font from a stream.
    /// </summary>
    /// <param name="stream">The stream containing font data.</param>
    /// <returns>A new font instance.</returns>
    public static HarfRustFont FromStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return new HarfRustFont(memoryStream.ToArray());
    }

    /// <summary>
    /// Gets the font's units per em value.
    /// </summary>
    public int UnitsPerEm
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.harfrust_font_units_per_em(_handle);
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

        var glyphBuffer = NativeMethods.harfrust_shape(_handle, buffer.ConsumeHandle());
        if (glyphBuffer == null)
        {
            throw new InvalidOperationException("Shaping failed.");
        }

        return new HarfRustGlyphBuffer(glyphBuffer);
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

        // Map features
        Bindings.HarfRustFeature[]? nativeFeatures = null;
        if (features != null && features.Length > 0)
        {
            nativeFeatures = new Bindings.HarfRustFeature[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                nativeFeatures[i] = new Bindings.HarfRustFeature
                {
                    tag = features[i].Tag,
                    value = features[i].Value,
                    start = features[i].Start,
                    end = features[i].End
                };
            }
        }

        // Map variations
        Bindings.HarfRustVariation[]? nativeVariations = null;
        if (variations != null && variations.Length > 0)
        {
            nativeVariations = new Bindings.HarfRustVariation[variations.Length];
            for (int i = 0; i < variations.Length; i++)
            {
                nativeVariations[i] = new Bindings.HarfRustVariation
                {
                    tag = variations[i].Tag,
                    value = variations[i].Value
                };
            }
        }

        Bindings.HarfRustGlyphBuffer* glyphBuffer;
        
        fixed (Bindings.HarfRustFeature* featPtr = nativeFeatures)
        fixed (Bindings.HarfRustVariation* varPtr = nativeVariations)
        {
             glyphBuffer = NativeMethods.harfrust_shape_full(
                _handle, 
                buffer.ConsumeHandle(), 
                featPtr,
                nativeFeatures == null ? 0 : (uint)nativeFeatures.Length,
                varPtr,
                nativeVariations == null ? 0 : (uint)nativeVariations.Length
            );
        }

        if (glyphBuffer == null)
        {
            throw new InvalidOperationException("Shaping failed.");
        }

        return new HarfRustGlyphBuffer(glyphBuffer);
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
            if (_handle != null)
            {
                NativeMethods.harfrust_font_free(_handle);
                _handle = null;
            }
            _disposed = true;
        }
    }
}
