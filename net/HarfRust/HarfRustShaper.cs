using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HarfRust;

/// <summary>
/// Provides advanced shaping capabilities, including font fallback.
/// </summary>
public static class HarfRustShaper
{
    /// <summary>
    /// Shapes text using a primary font and a list of fallback fonts.
    /// Glyphs missing in the primary font (Glyph ID 0) will be reshaped using the fallback fonts.
    /// </summary>
    /// <param name="text">The text to shape.</param>
    /// <param name="primaryFont">The primary font.</param>
    /// <param name="fallbackFonts">Ordered list of fallback fonts.</param>
    /// <param name="features">OpenType features to apply.</param>
    /// <param name="variations">Variable font axis settings.</param>
    /// <returns>A combined array of shaped glyphs.</returns>
    public static ShapedGlyph[] ShapeWithFallback(
        string text,
        HarfRustFont primaryFont,
        IEnumerable<HarfRustFont>? fallbackFonts,
        Feature[]? features = null,
        Variation[]? variations = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(primaryFont);

        var fonts = new List<HarfRustFont> { primaryFont };
        if (fallbackFonts != null)
        {
            fonts.AddRange(fallbackFonts);
        }

        using var session = new HarfRustShapeSession(primaryFont.Backend);
        return ShapeRecursive(text, 0, text.Length, fonts, 0, features, variations, session);
    }

    private static ShapedGlyph[] ShapeRecursive(
        string fullText,
        int start,
        int length,
        List<HarfRustFont> fonts,
        int fontIndex,
        Feature[]? features,
        Variation[]? variations,
        HarfRustShapeSession session)
    {
        if (length == 0) return Array.Empty<ShapedGlyph>();

        var font = fonts[fontIndex];

        // Shape this segment
        List<ShapedGlyph> shapedGlyphs;

        using (var result = session.Shape(font, fullText.AsSpan(start, length), features, variations, guessSegmentProperties: true))
        {
            var infos = result.GlyphInfos;
            var positions = result.GlyphPositions;

            shapedGlyphs = new List<ShapedGlyph>(infos.Length);

            // Convert to ShapedGlyph and adjust clusters while the result is alive.
            for (int i = 0; i < infos.Length; i++)
            {
                shapedGlyphs.Add(new ShapedGlyph(
                    infos[i].GlyphId,
                    infos[i].Cluster + (uint)start, // Adjust cluster relative to full text
                    positions[i].XAdvance,
                    positions[i].YAdvance,
                    positions[i].XOffset,
                    positions[i].YOffset,
                    font
                ));
            }
        }

        // If this is the last font, or no missing glyphs, return results
        if (fontIndex >= fonts.Count - 1)
        {
            return shapedGlyphs.ToArray();
        }

        // Check for missing glyphs (ID 0)
        // We need to group contiguous runs of missing glyphs to minimize reshaping calls.
        
        var finalResult = new List<ShapedGlyph>();
        
        int i_glyph = 0;
        while (i_glyph < shapedGlyphs.Count)
        {
            if (shapedGlyphs[i_glyph].GlyphId != 0)
            {
                finalResult.Add(shapedGlyphs[i_glyph]);
                i_glyph++;
                continue;
            }

            // Found a missing glyph. Identify the run of missing glyphs.
            int runStart = i_glyph;
            while (i_glyph < shapedGlyphs.Count && shapedGlyphs[i_glyph].GlyphId == 0)
            {
                i_glyph++;
            }
            // run is [runStart, i_glyph)

            // Calculate source range for this run
            uint minCluster = uint.MaxValue;
            uint maxCluster = 0;
            
            for (int k = runStart; k < i_glyph; k++)
            {
                var c = shapedGlyphs[k].Cluster;
                if (c < minCluster) minCluster = c;
                if (c > maxCluster) maxCluster = c;
            }

            // We need to know the length of the character at maxCluster to find the end.
            // Since working with native strings, check for surrogate pairs.
            int endOffset = (int)maxCluster;
            if (endOffset < fullText.Length)
            {
                if (char.IsHighSurrogate(fullText[endOffset]))
                {
                    endOffset += 2;
                }
                else
                {
                    endOffset += 1;
                }
            }
            
            int subStart = (int)minCluster;
            int subLength = endOffset - subStart;

            // Recurse with next font
            var fallbackResult = ShapeRecursive(
                fullText, 
                subStart, 
                subLength, 
                fonts, 
                fontIndex + 1, 
                features, 
                variations,
                session
            );

            finalResult.AddRange(fallbackResult);
        }

        return finalResult.ToArray();
    }
}
