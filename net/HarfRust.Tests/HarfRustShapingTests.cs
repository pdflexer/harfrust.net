using Xunit;

namespace HarfRust.Tests;

public class HarfRustShapingTests
{
    private static byte[]? _testFontData;
    
    private static byte[] GetTestFontData()
    {
        if (_testFontData != null)
            return _testFontData;
            
        // Try to find a system font for testing
        var possiblePaths = new[]
        {
            // Windows
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\tahoma.ttf",
            // Linux (Ubuntu/Debian standard paths)
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
            // macOS
            "/Library/Fonts/Arial.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf", 
            "/System/Library/Fonts/Helvetica.ttc"
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _testFontData = File.ReadAllBytes(path);
                return _testFontData;
            }
        }
        
        throw new InvalidOperationException("No system font available for testing");
    }
    
    [Fact]
    public void Font_FromData_LoadsSuccessfully()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        // If we get here without exception, the font loaded successfully
    }
    
    [Fact]
    public void Font_InvalidData_ThrowsArgumentException()
    {
        var invalidData = new byte[] { 0, 1, 2, 3, 4, 5 };
        Assert.Throws<ArgumentException>(() => new HarfRustFont(invalidData));
    }
    
    [Fact]
    public void Font_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HarfRustFont(null!));
    }
    
    [Fact]
    public void Font_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new HarfRustFont(Array.Empty<byte>()));
    }
    
    [Fact]
    public void Font_UnitsPerEm_ReturnsValidValue()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        
        var unitsPerEm = font.UnitsPerEm;
        
        // Common values are 1000 (PostScript) or 2048 (TrueType)
        Assert.True(unitsPerEm >= 16 && unitsPerEm <= 16384);
    }
    
    [Fact]
    public void Shape_SimpleText_ReturnsGlyphs()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("Hello");
        
        using var result = font.Shape(buffer);
        
        Assert.Equal(5, result.Length);
        Assert.Equal(5, result.GlyphInfos.Length);
        Assert.Equal(5, result.GlyphPositions.Length);
    }
    
    [Fact]
    public void Shape_GlyphInfos_HaveValidData()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("AB");
        
        using var result = font.Shape(buffer);
        
        var infos = result.GlyphInfos;
        
        // Glyph IDs should be non-zero for real characters
        Assert.True(infos[0].GlyphId > 0);
        Assert.True(infos[1].GlyphId > 0);
        
        // Clusters should match character positions
        Assert.Equal(0u, infos[0].Cluster);
        Assert.Equal(1u, infos[1].Cluster);
    }
    
    [Fact]
    public void Shape_GlyphPositions_HaveValidAdvances()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("W");  // W is usually a wide character
        
        using var result = font.Shape(buffer);
        
        var positions = result.GlyphPositions;
        
        // Horizontal advance should be positive for most glyphs
        Assert.True(positions[0].XAdvance > 0);
    }
    
    [Fact]
    public void Shape_EmptyBuffer_ReturnsEmptyResult()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        // Don't add any text
        
        using var result = font.Shape(buffer);
        
        Assert.Equal(0, result.Length);
    }
    
    [Fact]
    public void GlyphBuffer_IntoBuffer_ReturnsReusableBuffer()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("First");
        var result1 = font.Shape(buffer);
        
        // Convert glyph buffer back to unicode buffer (consumes result1)
        using var buffer2 = result1.IntoBuffer();
        
        // Add new text and shape again
        buffer2.AddString("Second");
        using var result2 = font.Shape(buffer2);
        
        Assert.Equal(6, result2.Length); // "Second" has 6 characters
    }
    
    [Fact]
    public void Buffer_ConsumedByShape_CannotBeReused()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        var buffer = new HarfRustBuffer();
        
        buffer.AddString("Test");
        using var result = font.Shape(buffer);
        
        // Buffer was consumed, should throw
        Assert.Throws<InvalidOperationException>(() => buffer.AddString("More"));
        Assert.Throws<InvalidOperationException>(() => _ = buffer.Length);
    }
    
    [Fact]
    public void Font_Dispose_CanBeCalledMultipleTimes()
    {
        var fontData = GetTestFontData();
        var font = new HarfRustFont(fontData);
        font.Dispose();
        font.Dispose(); // Should not throw
    }
    
    [Fact]
    public void GlyphBuffer_Dispose_CanBeCalledMultipleTimes()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        buffer.AddString("Test");
        
        var result = font.Shape(buffer);
        result.Dispose();
        result.Dispose(); // Should not throw
    }

    [Fact]
    public void Shape_WithFeatures_DoesNotCrash()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("fi");
        
        // Disable ligatures "liga" = 0
        var features = new[] { new Feature("liga", 0) };
        
        using var result = font.Shape(buffer, features);
        
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Shape_WithVariations_DoesNotCrash()
    {
        var fontData = GetTestFontData();
        using var font = new HarfRustFont(fontData);
        using var buffer = new HarfRustBuffer();
        
        buffer.AddString("Hello");
        
        var variations = new[] { new Variation("wght", 700.0f) };
        
        using var result = font.Shape(buffer, null, variations);
        
        Assert.True(result.Length > 0);
    }
}
