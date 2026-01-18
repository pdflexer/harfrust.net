namespace HarfRust;

/// <summary>
/// Represents a shaped glyph with its associated position and source font.
/// </summary>
public struct ShapedGlyph
{
    /// <summary>
    /// The glyph ID in the font.
    /// </summary>
    public uint GlyphId { get; set; }

    /// <summary>
    /// The cluster index (byte offset in UTF-8 text).
    /// </summary>
    public uint Cluster { get; set; }

    /// <summary>
    /// The horizontal advance width.
    /// </summary>
    public int XAdvance { get; set; }

    /// <summary>
    /// The vertical advance height.
    /// </summary>
    public int YAdvance { get; set; }

    /// <summary>
    /// The horizontal offset.
    /// </summary>
    public int XOffset { get; set; }

    /// <summary>
    /// The vertical offset.
    /// </summary>
    public int YOffset { get; set; }

    /// <summary>
    /// The font used to shape this glyph.
    /// </summary>
    public HarfRustFont Font { get; set; }

    public ShapedGlyph(uint glyphId, uint cluster, int xAdvance, int yAdvance, int xOffset, int yOffset, HarfRustFont font)
    {
        GlyphId = glyphId;
        Cluster = cluster;
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
        Font = font;
    }
}
