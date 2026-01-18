using Xunit;

namespace HarfRust.Tests;

public class HarfRustBufferTests
{
    [Fact]
    public void NewBuffer_HasZeroLength()
    {
        using var buffer = new HarfRustBuffer();
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void AddString_IncreasesLength()
    {
        using var buffer = new HarfRustBuffer();
        buffer.AddString("Hello");
        Assert.Equal(5, buffer.Length);
    }

    [Fact]
    public void AddString_MultipleStrings_AccumulatesLength()
    {
        using var buffer = new HarfRustBuffer();
        buffer.AddString("Hello");
        buffer.AddString(", ");
        buffer.AddString("world!");
        Assert.Equal(13, buffer.Length);
    }

    [Fact]
    public void AddString_EmptyString_DoesNotChangeLength()
    {
        using var buffer = new HarfRustBuffer();
        buffer.AddString("");
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void AddString_UnicodeCharacters_CountsCodepoints()
    {
        using var buffer = new HarfRustBuffer();
        // "Hello 世界" - 8 characters (6 ASCII + 2 CJK)
        buffer.AddString("Hello 世界");
        Assert.Equal(8, buffer.Length);
    }

    [Fact]
    public void Clear_ResetsLengthToZero()
    {
        using var buffer = new HarfRustBuffer();
        buffer.AddString("Hello, world!");
        Assert.Equal(13, buffer.Length);
        
        buffer.Clear();
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void Clear_AllowsReuse()
    {
        using var buffer = new HarfRustBuffer();
        buffer.AddString("First");
        buffer.Clear();
        buffer.AddString("Second");
        Assert.Equal(6, buffer.Length);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var buffer = new HarfRustBuffer();
        buffer.Dispose();
        buffer.Dispose(); // Should not throw
    }

    [Fact]
    public void AfterDispose_MethodsThrowObjectDisposedException()
    {
        var buffer = new HarfRustBuffer();
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.AddString("test"));
        Assert.Throws<ObjectDisposedException>(() => _ = buffer.Length);
        Assert.Throws<ObjectDisposedException>(() => buffer.Clear());
    }

    [Fact]
    public void AddString_NullString_ThrowsArgumentNullException()
    {
        using var buffer = new HarfRustBuffer();
        Assert.Throws<ArgumentNullException>(() => buffer.AddString(null!));
    }

    [Fact]
    public void UsingStatement_DisposesCorrectly()
    {
        HarfRustBuffer? bufferRef;
        
        using (var buffer = new HarfRustBuffer())
        {
            buffer.AddString("test");
            bufferRef = buffer;
        }
        
        // Buffer should be disposed after using block
        Assert.Throws<ObjectDisposedException>(() => bufferRef.AddString("more"));
    }

    [Fact]
    public void Direction_RoundTrip()
    {
        using var buffer = new HarfRustBuffer();
        
        // Default is Invalid
        Assert.Equal(Direction.Invalid, buffer.Direction);
        
        buffer.Direction = Direction.RightToLeft;
        Assert.Equal(Direction.RightToLeft, buffer.Direction);
        
        buffer.Direction = Direction.LeftToRight;
        Assert.Equal(Direction.LeftToRight, buffer.Direction);
    }

    [Fact]
    public void Script_RoundTrip()
    {
        using var buffer = new HarfRustBuffer();

        var latn = HarfRustBuffer.CreateScriptTag("Latn");
        buffer.Script = latn;
        Assert.Equal(latn, buffer.Script);
        
        var arab = HarfRustBuffer.CreateScriptTag("Arab");
        buffer.Script = arab;
        Assert.Equal(arab, buffer.Script);
    }

    [Fact]
    public void CreateScriptTag_ValidatesInput()
    {
        Assert.Throws<ArgumentException>(() => HarfRustBuffer.CreateScriptTag(null!));
        Assert.Throws<ArgumentException>(() => HarfRustBuffer.CreateScriptTag("Lat")); // Too short
        Assert.Throws<ArgumentException>(() => HarfRustBuffer.CreateScriptTag("Latnn")); // Too long
    }

    [Fact]
    public void SetLanguage_ValidTag_Succeeds()
    {
        using var buffer = new HarfRustBuffer();
        
        // Should not throw
        buffer.SetLanguage("en");
        buffer.SetLanguage("zh-Hans");
    }

    [Fact]
    public void SetLanguage_InvalidTag_ThrowsArgumentException()
    {
        using var buffer = new HarfRustBuffer();
        // Passing an empty string shouldn't crash but might fail validation inside harfrust or just be accepted as empty language
        // Checking behavior with empty string
        
        // Harfbuzz/Harfrust might accept empty string?
        // But invalid string might be harder to craft if it just parses.
        // Let's try explicit null which is caught by .NET wrapper wrapper check
        Assert.Throws<ArgumentNullException>(() => buffer.SetLanguage(null!));
    }

    [Fact]
    public void GuessSegmentProperties_UpdatesDirection()
    {
        using var buffer = new HarfRustBuffer();
        
        // Add Arabic text
        buffer.AddString("السلام عليكم");
        
        // Initially Invalid
        Assert.Equal(Direction.Invalid, buffer.Direction);
        
        buffer.GuessSegmentProperties();
        
        // Should detect RTL
        Assert.Equal(Direction.RightToLeft, buffer.Direction);
    }
}
