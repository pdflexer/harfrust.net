using System.Linq;
using Xunit;

namespace HarfRust.Tests;

public class TextAnalyzerTests
{
    [Fact]
    public void CountTextElements_BasicLatin_ReturnsLength()
    {
        string text = "ABC";
        Assert.Equal(3, TextAnalyzer.CountTextElements(text));
    }

    [Fact]
    public void CountTextElements_WithEmoji_CountsVisualCharacters()
    {
        // "A" (1) + "😀" (2) + "B" (1) = 4 chars, 3 elements
        string text = "A😀B";
        Assert.Equal(3, TextAnalyzer.CountTextElements(text));
    }

    [Fact]
    public void CountTextElements_WithCombiningDesign_CountsBaseAndMarkAsOne()
    {
        // 'e' + Acute Accent
        string text = "e\u0301"; 
        Assert.Equal(1, TextAnalyzer.CountTextElements(text));
    }

    [Fact]
    public void GetTextElementIndices_ReturnsStartOffsets()
    {
        string text = "A😀B";
        var indices = TextAnalyzer.GetTextElementIndices(text);
        
        Assert.Equal(new[] { 0, 1, 3 }, indices.ToArray());
    }

    [Fact]
    public void MapCharToElement_MapsCorrectly()
    {
        string text = "A😀B";
        // 0: 'A' -> Element 0
        // 1: '😀' (High) -> Element 1
        // 2: '😀' (Low)  -> Element 1
        // 3: 'B' -> Element 2

        Assert.Equal(0, TextAnalyzer.GetTextElementIndexFromCharIndex(text, 0));
        Assert.Equal(1, TextAnalyzer.GetTextElementIndexFromCharIndex(text, 1));
        Assert.Equal(1, TextAnalyzer.GetTextElementIndexFromCharIndex(text, 2));
        Assert.Equal(2, TextAnalyzer.GetTextElementIndexFromCharIndex(text, 3));
    }

    [Fact]
    public void MapElementToChar_MapsToStart()
    {
        string text = "A😀B";
        Assert.Equal(0, TextAnalyzer.GetCharIndexFromTextElementIndex(text, 0));
        Assert.Equal(1, TextAnalyzer.GetCharIndexFromTextElementIndex(text, 1));
        Assert.Equal(3, TextAnalyzer.GetCharIndexFromTextElementIndex(text, 2));
        Assert.Equal(4, TextAnalyzer.GetCharIndexFromTextElementIndex(text, 3)); // End
    }
}
