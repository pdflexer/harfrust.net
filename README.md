# HarfRust.NET

High-performance .NET bindings for the [harfrust](https://github.com/dfrg/harfrust) text shaping engine (a Rust port of HarfBuzz). HarfRust.NET allows you to convert Unicode text into positioned glyphs for rendering, supporting complex scripts, OpenType features, variable fonts, and font fallback.

## Features

- **High Performance**: Native UTF-16 support eliminates string conversion overhead. Zero-allocation interop via pinning.
- **Cross-Platform**: Packages natively for Windows and Linux (via NuGet).
- **Core Shaping**: Full HarfBuzz capabilities including script/direction detection and shaping.
- **Font Fallback**: Built-in support for resolving missing glyphs using a list of fallback fonts.
- **OpenType Features**: Easy control over ligatures, kerning, small caps, and stylistic sets.
- **Variable Fonts**: Support for variable axes (Weight, Width, Slant, etc.).

## Installation

Install via NuGet:

```bash
dotnet add package HarfRust
```

## Quick Start

### Basic Shaping

```csharp
using HarfRust;

// 1. Load a font
using var font = HarfRustFont.FromFile("path/to/font.ttf");

// 2. Create a buffer and add text
using var buffer = new HarfRustBuffer();
buffer.AddString("Hello World");
buffer.GuessSegmentProperties(); // Auto-detect direction, script, language

// 3. Shape the text
// Note: Shape consumes the buffer to transfer ownership to the result
using var result = font.Shape(buffer);

// 4. Read results
foreach (var info in result.GlyphInfos)
{
    Console.WriteLine($"Glyph ID: {info.GlyphId}, Cluster: {info.Cluster}");
}

foreach (var pos in result.GlyphPositions)
{
    Console.WriteLine($"Advance: {pos.XAdvance}, Offset: {pos.XOffset}, {pos.YOffset}");
}
```

### Font Fallback

Handle complex text mixing multiple scripts (e.g., Latin + Emoji) automatically.

```csharp
using HarfRust;

var arial = HarfRustFont.FromFile("arial.ttf");
var emoji = HarfRustFont.FromFile("seguiemj.ttf");

// Shape text containing characters not in Arial
var glyphs = HarfRustShaper.ShapeWithFallback(
    "Hello 😀", 
    arial, 
    new[] { emoji }
);

foreach (var glyph in glyphs)
{
    Console.WriteLine($"Glyph {glyph.GlyphId} from {glyph.Font}");
}
```

### Advanced Features

#### OpenType Features

```csharp
var features = new[] 
{ 
    Feature.StandardLigatures(false), // Disable ligatures
    Feature.SmallCaps(true),          // Enable small caps
    Feature.StylisticSet(1, true)     // Enable SS01
};

using var result = font.Shape(buffer, features);
```

#### Variable Fonts

```csharp
var variations = new[]
{
    Variation.Weight(700f), // Bold
    Variation.Width(85f)    // Condensed
};

using var result = font.Shape(buffer, variations: variations);
```

## Building from Source

To build the solution locally, you need:
- .NET 8.0 or 10.0 SDK
- Rust (cargo)

```bash
# 1. Build Native Library (Windows)
cd rust
cargo build --release

# 2. Build .NET Solution
cd ../net
dotnet build -c Release
```

For Linux builds, ensure you have the appropriate toolchain or use the provided Docker/GitHub Actions workflow.
