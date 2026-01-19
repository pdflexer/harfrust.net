using System;
using System.IO;
using System.Linq;
using Xunit;
using HarfRust;

namespace HarfRust.Tests;

public class FfiShaperTests : ShaperTestsBase<FfiBackendFixture>
{
    public FfiShaperTests(FfiBackendFixture fixture) : base(fixture) { }
}

public class WasmShaperTests : ShaperTestsBase<WasmBackendFixture>
{
    public WasmShaperTests(WasmBackendFixture fixture) : base(fixture) { }
}

public abstract class ShaperTestsBase<TFixture> : IClassFixture<TFixture> where TFixture : BackendFixture
{
    protected readonly TFixture Fixture;
    protected IHarfRustBackend Backend => Fixture.Backend;

    protected ShaperTestsBase(TFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public void ShapeWithFallback_UsesFallbackFontForMissingGlyphs()
    {
        HarfRustBackend.Current = Backend;

        string arialPath = @"C:\Windows\Fonts\arial.ttf";
        string emojiPath = @"C:\Windows\Fonts\seguiemj.ttf";
        
        // Skip test if generic fonts not available (e.g. non-Windows environment)
        if (!File.Exists(arialPath) || !File.Exists(emojiPath))
        {
            return;
        }

        using var arial = HarfRustFont.FromFile(arialPath, Backend);
        using var emoji = HarfRustFont.FromFile(emojiPath, Backend);

        // "A" supported by Arial, "😀" (U+1F600) supported by Segoe UI Emoji
        string text = "A😀"; 
        
        var results = HarfRustShaper.ShapeWithFallback(text, arial, new[] { emoji });
        
        Assert.NotNull(results);
        Assert.True(results.Length >= 2, "Should have at least 2 glyphs");

        // Verify fonts
        // We expect the first glyph(s) (for 'A') to use Arial
        // We expect the last glyph(s) (for '😀') to use Emoji font
        
        var firstGlyph = results[0];
        Assert.Same(arial, firstGlyph.Font);
        
        var lastGlyph = results[results.Length - 1];
        Assert.Same(emoji, lastGlyph.Font);
        
        // Verify cluster indices are correct
        // 'A' is at 0 (1 byte). '😀' is at 1 (4 bytes: F0 9F 98 80)
        // So 'A' cluster 0. '😀' cluster 1.
        
        Assert.Equal(0u, firstGlyph.Cluster);
        Assert.Equal(1u, lastGlyph.Cluster);
    }

    [Fact]
    public void ShapeWithFallback_NoFallbackNeeded()
    {
        HarfRustBackend.Current = Backend;

        string arialPath = @"C:\Windows\Fonts\arial.ttf";
        if (!File.Exists(arialPath)) return;

        using var arial = HarfRustFont.FromFile(arialPath, Backend);
        
        string text = "Hello";
        var results = HarfRustShaper.ShapeWithFallback(text, arial, null);
        
        Assert.True(results.Length > 0);
    }

    [Fact]
    public void ShapeWithFallback_MultibyteCharacters_OffsetsAreTraceable()
    {
        HarfRustBackend.Current = Backend;

        string arialPath = @"C:\Windows\Fonts\arial.ttf";
        string emojiPath = @"C:\Windows\Fonts\seguiemj.ttf";

        if (!File.Exists(arialPath) || !File.Exists(emojiPath))
            return;

        using var primary = HarfRustFont.FromFile(arialPath, Backend); 
        using var fallback = HarfRustFont.FromFile(emojiPath, Backend);

        // String: "A😀😀B😀C😀"
        // 'A' (primary) -> 0 (1 char)
        // '😀' (fallback) -> 1 (2 chars)
        // '😀' (fallback) -> 3 (2 chars)
        // 'B' (primary) -> 5 (1 char)
        // '😀' (fallback) -> 6 (2 chars)
        // 'C' (primary) -> 8 (1 char)
        // '😀' (fallback) -> 9 (2 chars)
        string text = "A😀😀B😀C😀";

        var results = HarfRustShaper.ShapeWithFallback(text, primary, new[] { fallback });

        Assert.True(results.Length >= 7);

        // Helper to check glyph
        void CheckGlyph(uint expectedCluster, HarfRustFont expectedFont)
        {
            var g = results.First(r => r.Cluster == expectedCluster);
            Assert.Same(expectedFont, g.Font);
        }

        CheckGlyph(0, primary);   // A
        CheckGlyph(1, fallback);  // 😀
        CheckGlyph(3, fallback);  // 😀
        CheckGlyph(5, primary);   // B
        CheckGlyph(6, fallback);  // 😀
        CheckGlyph(8, primary);   // C
        CheckGlyph(9, fallback);  // 😀
    }
}
