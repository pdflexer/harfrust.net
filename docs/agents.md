# HarfRust.NET Agent Guide

HarfRust.NET is a high-performance .NET wrapper for the `harfrust` text shaping engine (a Rust port of HarfBuzz). Use this library to convert Unicode text into positioned glyphs for rendering.

## Core Components

| Class | Description | Lifecycle |
|-------|-------------|-----------|
| `HarfRustFont` | Represents a font file (TTF/OTF). | `IDisposable` - Dispose when done. |
| `HarfRustBuffer` | Holds input text and shaping properties. | `IDisposable` - Consumed by `Shape()`. |
| `HarfRustGlyphBuffer` | Contains the shaping result (glyphs & positions). | `IDisposable` - Dispose when done. |

## Quick Start

```csharp
using HarfRust;

// 1. Load a font
using var font = HarfRustFont.FromFile("path/to/font.ttf");

// 2. Create and populate a buffer
using var buffer = new HarfRustBuffer();
buffer.AddString("Hello World");
buffer.GuessSegmentProperties(); // Auto-detect direction/script/lang

// 3. Shape the text
using var result = font.Shape(buffer); // Buffer is consumed here!

// 4. Read results
foreach (var info in result.GlyphInfos)
{
    Console.WriteLine($"Glyph ID: {info.GlyphId}, Cluster: {info.Cluster}");
}
foreach (var pos in result.GlyphPositions)
{
    Console.WriteLine($"Advance: {pos.XAdvance}, Offset: {pos.XOffset}");
}
```

## Advanced Usage

### 1. explicit Configuration
Set properties manually for complex scripts or to override auto-detection.

```csharp
using var buffer = new HarfRustBuffer();
buffer.AddString("مرحبا");
buffer.Direction = Direction.RightToLeft;
buffer.Script = HarfRustBuffer.CreateScriptTag("Arab");
buffer.SetLanguage("ar");
```

### 2. OpenType Features
Enable/disable features like ligatures (`liga`), kerning (`kern`), etc.

```csharp
var features = new[] 
{ 
    Feature.StandardLigatures(false), // Disable standard ligatures
    Feature.SmallCaps(true)           // Enable small caps
};

using var result = font.Shape(buffer, features);
```

### 3. Variable Fonts
Set variation axes like weight (`wght`) or width (`wdth`).

```csharp
var variations = new[]
{
    Variation.Weight(700f), // Bold
    Variation.Width(85f)    // Condensed
};

using var result = font.Shape(buffer, variations: variations);
```

### 4. Font Fallback
Automatically use secondary fonts when glyphs are missing in the primary font.

```csharp
var primary = HarfRustFont.FromFile("arial.ttf");
var emoji = HarfRustFont.FromFile("seguiemj.ttf");

// "A" maps to Arial, "😀" maps to Emoji
// Returns ShapedGlyph[] (not HarfRustGlyphBuffer)
var glyphs = HarfRustShaper.ShapeWithFallback("A😀", primary, new[] { emoji });

foreach (var g in glyphs)
{
    Console.WriteLine($"Glyph: {g.GlyphId} from Font: {g.Font}");
}
```

## Best Practices

*   **Resource Management**: Always use `using` statements. These objects wrap native handles.
*   **Buffer Reuse**: `Shape()` consumes the buffer. To reuse the allocation, use `result.IntoBuffer()`:
    ```csharp
    using var result = font.Shape(buffer);
    // Process result...
    using var reusedBuffer = result.IntoBuffer(); // Get fresh buffer
    ```
*   **Performance**: Reuse `HarfRustFont` instances across multiple shaping calls.
