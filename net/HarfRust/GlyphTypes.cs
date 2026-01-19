using System.Runtime.InteropServices;

namespace HarfRust;

/// <summary>
/// Glyph information after text shaping.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct GlyphInfo
{
    /// <summary>
    /// The glyph ID in the font.
    /// </summary>
    public readonly uint GlyphId;

    /// <summary>
    /// The cluster index (position in original text).
    /// </summary>
    public readonly uint Cluster;

    /// <summary>
    /// Creates a new glyph info.
    /// </summary>
    public GlyphInfo(uint glyphId, uint cluster)
    {
        GlyphId = glyphId;
        Cluster = cluster;
    }
}

/// <summary>
/// Glyph positioning information after text shaping.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct GlyphPosition
{
    /// <summary>
    /// Horizontal advance after drawing this glyph.
    /// </summary>
    public readonly int XAdvance;

    /// <summary>
    /// Vertical advance after drawing this glyph.
    /// </summary>
    public readonly int YAdvance;

    /// <summary>
    /// Horizontal offset for drawing.
    /// </summary>
    public readonly int XOffset;

    /// <summary>
    /// Vertical offset for drawing.
    /// </summary>
    public readonly int YOffset;

    /// <summary>
    /// Creates a new glyph position.
    /// </summary>
    public GlyphPosition(int xAdvance, int yAdvance, int xOffset, int yOffset)
    {
        XAdvance = xAdvance;
        YAdvance = yAdvance;
        XOffset = xOffset;
        YOffset = yOffset;
    }
}
