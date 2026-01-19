# HarfRust.NET

High-performance .NET bindings for the [harfrust](https://github.com/harfbuzz/harfrust) text shaping engine (a Rust port of HarfBuzz). HarfRust.NET allows you to convert Unicode text into positioned glyphs for rendering, supporting complex scripts, OpenType features, variable fonts, and font fallback.

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
# Native Runtime (Required for default FFI backend)
dotnet add package HarfRust.Runtime.Ffi
# OR Wasmtime Backend, call `using new WasmtimeBackend();` to enable
dotnet add package HarfRust.Backend.Wasmtime
```

## Backends

HarfRust.NET separates the core API from the implementation backend.

1.  **FFI Backend** (`HarfRust.Runtime.Ffi`): Default. Uses native binaries. High performance.
2.  **WASM Backend** (`HarfRust.Backend.Wasmtime`): Uses WebAssembly version of HarfRust running in Wasmtime. Sandboxed and portable.

### Using WASM Backend

You can enable the WASM backend for a specific scope:

```csharp
using HarfRust.Backend.Wasmtime;

// ....

using (new WasmtimeBackend()) 
{
    // Inside this block, HarfRust uses the WASM backend automatically
    using var font = HarfRustFont.FromFile("font.ttf");
    var glyphs = font.Shape(buffer);
}
// Reverts to default backend here
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

### Understanding Glyph Positioning

When rendering text, two key metrics determine where each glyph is drawn:

- **XAdvance (Advance Width)**: The distance the "pen" or cursor moves *after* drawing the glyph. This determines the start position of the *next* glyph. It corresponds to the logical width of the character including spacing.
- **XOffset**: A visual shift applied to the glyph relative to the current pen position. This shifts where the glyph ink is drawn but *does not* affect the pen position for subsequent glyphs.

#### Diagram

```text
      Current Pen Position (Origin)
      |
      +-------------------------> [ Glyph Ink Drawn Here ]
      |        XOffset
      |
      |
      |------------------------------------------------------> Next Pen Position
                           XAdvance
```

In most cases (e.g., standard Latin text), `XOffset` is 0. It is commonly used for:
- **Mark Positioning**: Adjusting the position of accents or combining marks relative to the base character.
- **Improved Kerning**: Fine-tuning visual placement without changing logical flow (though usually `XAdvance` is adjusted for kerning).

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
