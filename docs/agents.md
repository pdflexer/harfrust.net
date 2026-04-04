# HarfRust.NET Agent Guide

HarfRust.NET is a high-performance .NET wrapper for the `harfrust` text shaping engine (a Rust port of HarfBuzz). Use this library to convert Unicode text into positioned glyphs for rendering.

## Installation

```bash
dotnet add package HarfRust
# Optional: Add native runtimes for default Native backend (Required if using Native backend)
dotnet add package HarfRust.Native
# Optional: Add Wasmtime backend
dotnet add package HarfRust.Wasmtime
```

## Core Components

| Class | Description | Lifecycle |
|-------|-------------|-----------|
| `HarfRustFont` | Represents a loaded font file (TTF/OTF/TTC). | `IDisposable` – Dispose when done. |
| `HarfRustBuffer` | Holds input text and shaping properties. | `IDisposable` – Consumed by `Shape()`. |
| `HarfRustShapeSession` | Reuses a shaping buffer across multiple operations. | `IDisposable` – Preferred for repeated shaping. |
| `HarfRustGlyphBuffer` | Contains the shaping result (glyphs & positions). | `IDisposable` – Dispose when done. |
| `HarfRustShaper` | Static class for advanced shaping with font fallback. | Static – No lifecycle management. |
| `TextAnalyzer` | Static utilities for grapheme cluster / Text Element mapping. | Static – No lifecycle management. |

## Quick Start

```csharp
using HarfRust;

// 1. Load a font
using var font = HarfRustFont.FromFile("path/to/font.ttf");

// 2. Create a reusable shaping session
using var session = new HarfRustShapeSession();

// 3. Shape the text
using var result = session.Shape(font, "Hello World"); // Auto-detects segment properties by default

// 4. Read results
foreach (var info in result.GlyphInfos)
{
    Console.WriteLine($"Glyph ID: {info.GlyphId}, Cluster: {info.Cluster}");
}
foreach (var pos in result.GlyphPositions)
{
    Console.WriteLine($"Advance: {pos.XAdvance}, Offset: ({pos.XOffset}, {pos.YOffset})");
}
```

---

## API Reference

### `HarfRustFont`

Represents a loaded font for text shaping. Owns a copy of font data internally.

#### Constructors & Factory Methods

| Method | Description |
|--------|-------------|
| `HarfRustFont(byte[] data)` | Create from TTF/OTF bytes. |
| `HarfRustFont(byte[] data, int index)` | Create from font bytes at a specific index (for TTC collections). |
| `HarfRustFont.FromFile(string path)` | Load font from file path. |
| `HarfRustFont.FromFile(string path, int index)` | Load from TTC/OTC collection at specific index. |
| `HarfRustFont.FromStream(Stream stream)` | Load font from stream (reads to memory). |

#### Methods

| Method | Description |
|--------|-------------|
| `HarfRustGlyphBuffer Shape(HarfRustBuffer buffer, Feature[]? features = null, Variation[]? variations = null)` | Shape text in buffer and return result. **Consumes the buffer.** |
| `HarfRustGlyphBuffer Shape(HarfRustBuffer buffer, ReadOnlySpan<Feature> features, ReadOnlySpan<Variation> variations = default)` | Span-based shaping overload for low-allocation callers. |
| `void Dispose()` | Release native resources. |

#### Exceptions
- `ArgumentNullException` – If data/path is null.
- `ArgumentException` – If data is invalid or index is out of range.
- `FileNotFoundException` – If file path doesn't exist.

---

### `HarfRustBuffer`

Accumulates text and shaping properties before shaping.

#### Constructor

```csharp
var buffer = new HarfRustBuffer();
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Direction` | `Direction` | Get/set text direction. |
| `Script` | `uint` | Get/set ISO 15924 script tag. Use `CreateScriptTag()`. |
| `Length` | `int` | Number of characters in the buffer. |

#### Methods

| Method | Description |
|--------|-------------|
| `void AddString(string text)` | Add text to the buffer. |
| `void Add(ReadOnlySpan<char> text)` | Add text from a span without first materializing a new string. |
| `void Clear()` | Clear all content, reset for reuse. |
| `void SetLanguage(string language)` | Set BCP 47 language tag (e.g., `"en"`, `"zh-Hans"`). |
| `void SetLanguage(ReadOnlySpan<char> language)` | Span-based language setter. |
| `void GuessSegmentProperties()` | Auto-detect and set direction, script, language from buffer content. |
| `static uint CreateScriptTag(string tag)` | Convert 4-char script tag (e.g., `"Latn"`) to uint. |
| `void Dispose()` | Release native resources. |

#### Exceptions
- `ObjectDisposedException` – If used after disposal.
- `InvalidOperationException` – If used after being consumed by `Shape()`.
- `ArgumentException` – For invalid language tags.

---

### `HarfRustGlyphBuffer`

The result of text shaping, containing glyph IDs and positions.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Length` | `int` | Number of glyphs in the result. |
| `GlyphInfos` | `ReadOnlySpan<GlyphInfo>` | Span of glyph information. Valid only while buffer is not disposed. |
| `GlyphPositions` | `ReadOnlySpan<GlyphPosition>` | Span of glyph positions. Valid only while buffer is not disposed. |

#### Methods

| Method | Description |
|--------|-------------|
| `HarfRustBuffer IntoBuffer()` | Convert back to a reusable `HarfRustBuffer`. Disposes this glyph buffer. Not available for glyph buffers owned by `HarfRustShapeSession`. |
| `void Dispose()` | Release native resources. |

---

### `HarfRustShapeSession`

The preferred API for repeated shaping. Owns a reusable backend buffer and automatically reacquires it when the returned `HarfRustGlyphBuffer` is disposed.

#### Constructor

```csharp
using var session = new HarfRustShapeSession();
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Direction` | `Direction` | Get/set text direction. |
| `Script` | `uint` | Get/set ISO 15924 script tag. |
| `Length` | `int` | Number of characters currently staged in the session buffer. |

#### Methods

| Method | Description |
|--------|-------------|
| `void Add(string text)` | Add text to the session buffer. |
| `void Add(ReadOnlySpan<char> text)` | Span-based input path for low-allocation shaping. |
| `void Clear()` | Clear the current buffer contents. |
| `void SetLanguage(string language)` | Set BCP 47 language tag. |
| `void SetLanguage(ReadOnlySpan<char> language)` | Span-based language setter. |
| `void GuessSegmentProperties()` | Auto-detect direction, script, and language. |
| `HarfRustGlyphBuffer Shape(HarfRustFont font)` | Shape the currently staged buffer contents. |
| `HarfRustGlyphBuffer Shape(HarfRustFont font, ReadOnlySpan<Feature> features, ReadOnlySpan<Variation> variations = default)` | Shape staged content with span-based feature/variation inputs. |
| `HarfRustGlyphBuffer Shape(HarfRustFont font, string text, ReadOnlySpan<Feature> features = default, ReadOnlySpan<Variation> variations = default, bool guessSegmentProperties = true)` | Convenience overload that clears, adds, optionally guesses properties, and shapes in one call. |
| `HarfRustGlyphBuffer Shape(HarfRustFont font, ReadOnlySpan<char> text, ReadOnlySpan<Feature> features = default, ReadOnlySpan<Variation> variations = default, bool guessSegmentProperties = true)` | Span-based convenience overload for repeated shaping. |
| `void Dispose()` | Release the reusable buffer. |

#### Important Behavior
- A session can only have one active `HarfRustGlyphBuffer` at a time.
- Dispose the returned glyph buffer before calling `Add`, `Clear`, or `Shape` again.
- Sessions are not thread-safe.

---

### `GlyphInfo` (struct)

Information about a shaped glyph.

| Property | Type | Description |
|----------|------|-------------|
| `GlyphId` | `uint` | The glyph ID in the font. `0` indicates a missing glyph (.notdef). |
| `Cluster` | `uint` | The cluster index (position in original text). Maps glyphs to source characters. |

---

### `GlyphPosition` (struct)

Position information for a shaped glyph.

| Property | Type | Description |
|----------|------|-------------|
| `XAdvance` | `int` | Horizontal advance after drawing (pen movement for next glyph). |
| `YAdvance` | `int` | Vertical advance (for vertical text). |
| `XOffset` | `int` | Horizontal offset for drawing (visual shift, doesn't affect pen). |
| `YOffset` | `int` | Vertical offset for drawing. |

**Units**: All values are in font design units. Divide by font's units-per-em and multiply by point size to get real dimensions.

---

### `Direction` (enum)

Text direction for shaping.

| Value | Description |
|-------|-------------|
| `Invalid` (0) | Initial, unset direction. |
| `LeftToRight` (4) | Left-to-right text (Latin, Cyrillic, etc.). |
| `RightToLeft` (5) | Right-to-left text (Arabic, Hebrew). |
| `TopToBottom` (6) | Top-to-bottom text (some CJK modes). |
| `BottomToTop` (7) | Bottom-to-top text. |

---

### `Feature` (struct)

OpenType feature settings.

#### Constructor

```csharp
Feature(string tag, uint value = 1)           // Applies to entire buffer
Feature(string tag, uint value, uint start, uint end)  // Applies to range [start, end]
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Tag` | `uint` | OpenType feature tag. |
| `Value` | `uint` | 0 = disabled, 1 = enabled. |
| `Start` | `uint` | Start index (0 = beginning). |
| `End` | `uint` | End index (`uint.MaxValue` = entire text). |

#### Factory Methods

| Method | Description |
|--------|-------------|
| `Feature.StandardLigatures(bool enable)` | `liga` – fi, fl, etc. |
| `Feature.DiscretionaryLigatures(bool enable)` | `dlig` – decorative ligatures. |
| `Feature.Kerning(bool enable)` | `kern` – pair-wise spacing. |
| `Feature.SmallCaps(bool enable)` | `smcp` – small capitals. |
| `Feature.StylisticSet(int setIndex, bool enable)` | `ss01`-`ss20` – stylistic alternates. |

#### Example: Custom Tag

```csharp
var oldstyleFigures = new Feature("onum", 1);  // Enable old-style numerals
```

---

### `Variation` (struct)

Variable font axis settings.

#### Constructor

```csharp
Variation(string tag, float value)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Tag` | `uint` | Axis tag (e.g., `wght`). |
| `Value` | `float` | Axis value. |

#### Factory Methods

| Method | Axis Tag | Typical Range |
|--------|----------|---------------|
| `Variation.Weight(float value)` | `wght` | 100 (Thin) – 900 (Black) |
| `Variation.Width(float value)` | `wdth` | 50 (Ultra-Condensed) – 200 (Ultra-Expanded) |
| `Variation.Slant(float value)` | `slnt` | -90 to 90 degrees |
| `Variation.OpticalSize(float value)` | `opsz` | Point size (e.g., 12, 72) |
| `Variation.Italic(float value)` | `ital` | 0 (Roman) – 1 (Italic) |

---

### `HarfRustShaper`

Static class for advanced shaping with font fallback.

#### Methods

| Method | Description |
|--------|-------------|
| `ShapedGlyph[] ShapeWithFallback(string text, HarfRustFont primaryFont, IEnumerable<HarfRustFont>? fallbackFonts, Feature[]? features = null, Variation[]? variations = null)` | Shape text, using fallback fonts for missing glyphs (GlyphId 0). |

**Note**: Returns `ShapedGlyph[]`, not `HarfRustGlyphBuffer`. Each glyph tracks which font it came from.

---

### `ShapedGlyph` (struct)

A glyph from `ShapeWithFallback()` with its source font.

| Property | Type | Description |
|----------|------|-------------|
| `GlyphId` | `uint` | Glyph ID in the font. |
| `Cluster` | `uint` | Cluster index in original text. |
| `XAdvance` | `int` | Horizontal advance. |
| `YAdvance` | `int` | Vertical advance. |
| `XOffset` | `int` | Horizontal offset. |
| `YOffset` | `int` | Vertical offset. |
| `Font` | `HarfRustFont` | The font used to shape this glyph. |

---

### `TextAnalyzer`

Static utilities for working with grapheme clusters (Text Elements).

#### Methods

| Method | Description |
|--------|-------------|
| `int CountTextElements(string text)` | Count visual characters (grapheme clusters). |
| `List<int> GetTextElementIndices(string text)` | Get char indices where each grapheme starts. |
| `int GetTextElementIndexFromCharIndex(string text, int charIndex)` | Map char index → grapheme index. |
| `int GetCharIndexFromTextElementIndex(string text, int elementIndex)` | Map grapheme index → char index. |

**Use Case**: Mapping cluster values from `GlyphInfo` to user-visible characters.

```csharp
// Example: "👨‍👩‍👧" is one grapheme but multiple chars
var text = "A👨‍👩‍👧B";
var count = TextAnalyzer.CountTextElements(text);  // 3 visual characters
var indices = TextAnalyzer.GetTextElementIndices(text);  // [0, 1, 9] char positions
```

---

## Backends

HarfRust.NET supports multiple backends for text shaping.

### 1. Native Backend (Default)
Uses native `harfrust` binaries via P/Invoke. 
- **Requires**: `HarfRust.Native` package referenced in your project.
- **Performance**: Fastest.
- **Platform**: Windows, Linux, macOS (x64/arm64).

### 2. Wasmtime Backend (WASM)
Runs the `harfrust` engine as a WebAssembly module using `Wasmtime`.
- **Requires**: `HarfRust.Wasmtime` package.
- **Performance**: Slower than FFI but sandboxed and platform-independent (wherever Wasmtime runs).
- **Usability**: Ideal for containerized environments or where native dynlibs are problematic.

### Switching Backends

You can switch the global backend for the current async context using `HarfRustBackend.Current`.

```csharp
using HarfRust.Wasmtime;

// Use Native backend by default (if HarfRust.Native is installed)
using var fontFfi = HarfRustFont.FromFile("font.ttf");

// Switch to WASM backend
using (new WasmtimeBackend()) // WasmtimeBackend automatically sets itself as Current
{
    // All fonts/buffers created here use the WASM backend
    using var fontWasm = HarfRustFont.FromFile("font.ttf");
    using var bufferWasm = new HarfRustBuffer();
    
    // Shaping happens in WASM
    var result = fontWasm.Shape(bufferWasm);
}
// Automatically reverts to Native backend after disposal
```

Alternatively, pass the backend explicitly:

```csharp
using var wasmBackend = new WasmtimeBackend();
using var font = HarfRustFont.FromFile("font.ttf", wasmBackend);
```

### Sharing Wasmtime Engine

You can share the `Wasmtime.Engine` across multiple `WasmtimeBackend` instances to reduce overhead (sharing JIT caches). The backend will **not** dispose the external engine; you are responsible for its lifecycle.

```csharp
using Wasmtime;

// Create a shared engine
using var engine = new Engine();

// Create multiple backends sharing the engine
using var backend1 = new WasmtimeBackend(engine);
using var backend2 = new WasmtimeBackend(engine);
```

---

## Advanced Usage Examples

### 1. Explicit Direction/Script/Language

```csharp
using var session = new HarfRustShapeSession();
session.Add("مرحبا".AsSpan());
session.Direction = Direction.RightToLeft;
session.Script = HarfRustBuffer.CreateScriptTag("Arab");
session.SetLanguage("ar".AsSpan());

using var result = session.Shape(font);
```

### 2. OpenType Features (Ligatures, Small Caps)

```csharp
var features = new[] 
{ 
    Feature.StandardLigatures(false),  // Disable "fi" ligatures
    Feature.SmallCaps(true),           // Enable small caps
    Feature.StylisticSet(3, true)      // Enable stylistic set 3
};

using var result = font.Shape(buffer, features);
```

### 3. Variable Fonts

```csharp
var variations = new[]
{
    Variation.Weight(700f),      // Bold
    Variation.Width(85f),        // Condensed
    Variation.OpticalSize(24f)   // Optimize for 24pt display
};

using var result = font.Shape(buffer, variations: variations);
```

### 4. Font Fallback (Mixed Scripts)

```csharp
var primary = HarfRustFont.FromFile("arial.ttf");
var emoji = HarfRustFont.FromFile("seguiemj.ttf");
var cjk = HarfRustFont.FromFile("notosanscjk.ttf");

// Glyphs missing in Arial fall back to emoji, then CJK
var glyphs = HarfRustShaper.ShapeWithFallback(
    "Hello 😀 你好",
    primary,
    new[] { emoji, cjk }
);

foreach (var g in glyphs)
{
    Console.WriteLine($"Glyph: {g.GlyphId} from Font: {g.Font}");
}
```

### 5. Buffer Reuse With `HarfRustShapeSession`

```csharp
using var session = new HarfRustShapeSession();

using (var result1 = session.Shape(font, "First"))
{
    // Process result1...
}

using (var result2 = session.Shape(font, "Second"))
{
    // Reuses the same backend buffer after result1 is disposed
}
```

### 6. Low-Level Buffer Reuse With `IntoBuffer()`

```csharp
using var buffer = new HarfRustBuffer();
buffer.Add("First".AsSpan());
buffer.GuessSegmentProperties();

using var result1 = font.Shape(buffer);  // Buffer consumed here
using var buffer2 = result1.IntoBuffer(); // result1 is now disposed

buffer2.Add("Second".AsSpan());
buffer2.GuessSegmentProperties();

using var result2 = font.Shape(buffer2);
```

### 7. Font Collections (TTC files)

```csharp
// Load specific font from a TTC collection
using var font = HarfRustFont.FromFile("msgothic.ttc", index: 1);
```

---

## Understanding Glyph Positioning

When rendering text, two key metrics determine where each glyph is drawn:

- **XAdvance**: Distance the pen moves *after* drawing the glyph. Determines start of next glyph.
- **XOffset**: Visual shift of glyph ink relative to pen. Does *not* move pen.

```
      Pen Position
      |
      +---(XOffset)---> [ Glyph Ink ]
      |
      |-----(XAdvance)--------------> Next Pen Position
```

In most Latin text, `XOffset` is 0. Common uses:
- **Combining marks**: Position accents over base characters.
- **Kerning adjustments**: Fine-tune visual placement.

---

## Best Practices

- **Always use `using`**: All core types wrap native handles and must be disposed.
- **Prefer sessions for repeated shaping**: `HarfRustShapeSession` is the intended reuse API.
- **Dispose glyph buffers promptly**: A shape session cannot be reused until its current `HarfRustGlyphBuffer` is disposed.
- **Buffer consumption**: `Shape()` still consumes `HarfRustBuffer`. Use `IntoBuffer()` only for low-level/manual reuse.
- **Reuse fonts**: `HarfRustFont` is expensive to create; share across shaping calls.
- **Check for GlyphId 0**: Indicates missing glyph (.notdef). Use font fallback or substitution.
- **Position units**: Values are in font design units. Scale by `point_size / units_per_em`.

---

## Error Handling

| Exception | Common Causes |
|-----------|---------------|
| `ObjectDisposedException` | Using disposed font/buffer. |
| `InvalidOperationException` | Using buffer after it was consumed by `Shape()`. |
| `ArgumentNullException` | Null text, data, or path. |
| `ArgumentException` | Invalid font data, empty data, or invalid language/script tag. |
| `FileNotFoundException` | Font file path doesn't exist. |
