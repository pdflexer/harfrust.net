using Xunit;

namespace HarfRust.Tests;

/// <summary>
/// Shaping tests using the Native backend.
/// </summary>
public class NativeShapingTests : ShapingTestsBase<NativeBackendFixture>
{
    public NativeShapingTests(NativeBackendFixture fixture) : base(fixture) { }
}


/// <summary>
/// Shaping tests using the Wasmtime (WASM) backend.
/// </summary>
public class WasmShapingTests : ShapingTestsBase<WasmBackendFixture>
{
    public WasmShapingTests(WasmBackendFixture fixture) : base(fixture) { }
}



/// <summary>
/// Abstract base class for shaping tests that run against any backend.
/// </summary>
/// <typeparam name="TFixture">The backend fixture type.</typeparam>
public abstract class ShapingTestsBase<TFixture> : IClassFixture<TFixture> where TFixture : BackendFixture
{
    protected readonly TFixture Fixture;
    protected IHarfRustBackend Backend => Fixture.Backend;

    protected ShapingTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public void Font_FromData_LoadsSuccessfully()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        Assert.True(font.UnitsPerEm > 0);
    }

    [Fact]
    public void Font_InvalidData_ThrowsArgumentException()
    {
        var invalidData = new byte[] { 0, 1, 2, 3, 4, 5 };
        Assert.Throws<ArgumentException>(() => new HarfRustFont(invalidData, Backend));
    }

    [Fact]
    public void Shape_SimpleText_ReturnsGlyphs()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Hello");
        
        using var result = font.Shape(buffer);
        
        Assert.Equal(5, result.Length);
        Assert.Equal(5, result.GlyphInfos.Length);
        Assert.Equal(5, result.GlyphPositions.Length);
    }

    [Fact]
    public void Shape_UnicodeText_ReturnsGlyphs()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Héllo Wörld");
        
        using var result = font.Shape(buffer);
        
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Buffer_SetDirection_Works()
    {
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.Direction = Direction.RightToLeft;
        
        Assert.Equal(Direction.RightToLeft, buffer.Direction);
    }

    [Fact]
    public void Buffer_SetScript_Works()
    {
        using var buffer = new HarfRustBuffer(Backend);
        var latnTag = HarfRustBuffer.CreateScriptTag("Latn");
        
        buffer.Script = latnTag;
        
        Assert.Equal(latnTag, buffer.Script);
    }

    [Fact]
    public void Buffer_AddString_IncreasesLength()
    {
        using var buffer = new HarfRustBuffer(Backend);
        
        Assert.Equal(0, buffer.Length);
        buffer.AddString("Test");
        Assert.Equal(4, buffer.Length);
    }

    [Fact]
    public void Buffer_Clear_ResetsLength()
    {
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Test");
        Assert.Equal(4, buffer.Length);
        
        buffer.Clear();
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void Shape_WithFeatures_Works()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Hello");
        
        var features = new[] { Feature.StandardLigatures(true) };
        using var result = font.Shape(buffer, features);
        
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GlyphBuffer_IntoBuffer_CreatesReusableBuffer()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Hello");
        
        using var glyphResult = font.Shape(buffer);
        using var reusedBuffer = glyphResult.IntoBuffer();
        
        // Can add new text to reused buffer
        reusedBuffer.AddString("World");
        Assert.Equal(5, reusedBuffer.Length);
    }

    [Fact]
    public void Shape_MultipleShapes_WorkCorrectly()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        
        for (int i = 0; i < 10; i++)
        {
            using var buffer = new HarfRustBuffer(Backend);
            buffer.AddString($"Test {i}");
            using var result = font.Shape(buffer);
            Assert.True(result.Length > 0);
        }
    }

    [Fact]
    public void Font_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HarfRustFont(null!, Backend));
    }

    [Fact]
    public void Font_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new HarfRustFont(Array.Empty<byte>(), Backend));
    }

    [Fact]
    public void Font_UnitsPerEm_ReturnsValidValue_Detailed()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        
        var unitsPerEm = font.UnitsPerEm;
        
        // Common values are 1000 (PostScript) or 2048 (TrueType)
        Assert.True(unitsPerEm >= 16 && unitsPerEm <= 16384);
    }

    [Fact]
    public void Shape_GlyphInfos_HaveValidData()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
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
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("W");  // W is usually a wide character
        
        using var result = font.Shape(buffer);
        
        var positions = result.GlyphPositions;
        
        // Horizontal advance should be positive for most glyphs
        Assert.True(positions[0].XAdvance > 0);
    }

    [Fact]
    public void Shape_EmptyBuffer_ReturnsEmptyResult()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        // Don't add any text
        
        using var result = font.Shape(buffer);
        
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Buffer_ConsumedByShape_CannotBeReused()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Test");
        using var result = font.Shape(buffer);
        
        // Buffer was consumed, should throw
        Assert.Throws<InvalidOperationException>(() => buffer.AddString("More"));
        Assert.Throws<InvalidOperationException>(() => _ = buffer.Length);
    }
    
    [Fact]
    public void Font_Dispose_CanBeCalledMultipleTimes()
    {
        var fontData = Fixture.GetTestFontData();
        var font = new HarfRustFont(fontData, Backend);
        font.Dispose();
        font.Dispose(); // Should not throw
    }
    
    [Fact]
    public void GlyphBuffer_Dispose_CanBeCalledMultipleTimes()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        buffer.AddString("Test");
        
        var result = font.Shape(buffer);
        result.Dispose();
        result.Dispose(); // Should not throw
    }

    [Fact]
    public void Shape_WithExplicitFeatures_Works()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("fi");
        
        // Disable ligatures "liga" = 0
        var features = new[] { new Feature("liga", 0) };
        
        using var result = font.Shape(buffer, features);
        
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void Shape_WithVariations_DoesNotCrash()
    {
        var fontData = Fixture.GetTestFontData();
        using var font = new HarfRustFont(fontData, Backend);
        using var buffer = new HarfRustBuffer(Backend);
        
        buffer.AddString("Hello");
        
        var variations = new[] { new Variation("wght", 700.0f) };
        
        using var result = font.Shape(buffer, null, variations);
        
        Assert.True(result.Length > 0);
    }
}
