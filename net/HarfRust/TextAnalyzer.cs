using System;
using System.Collections.Generic;
using System.Globalization;

namespace HarfRust;

/// <summary>
/// Provides utility methods for analyzing text structure, specifically mapping between
/// UTF-16 code units (char indices) and user-perceived characters (Text Elements / Grapheme Clusters).
/// </summary>
public static class TextAnalyzer
{
    /// <summary>
    /// Counts the number of Text Elements (Grapheme Clusters) in the string.
    /// This corresponds to the number of visual characters the user sees.
    /// </summary>
    public static int CountTextElements(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return new StringInfo(text).LengthInTextElements;
    }

    /// <summary>
    /// Gets the start index of each Text Element in the string.
    /// </summary>
    /// <param name="text">The input string.</param>
    /// <returns>A list of char indices where each Text Element begins.</returns>
    public static List<int> GetTextElementIndices(string text)
    {
        var indices = new List<int>();
        if (string.IsNullOrEmpty(text)) return indices;

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            indices.Add(enumerator.ElementIndex);
        }
        return indices;
    }

    /// <summary>
    /// Maps a UTF-16 char index to its corresponding Text Element index.
    /// If the char index points to the middle of a Text Element, it maps to that element's index.
    /// </summary>
    /// <param name="text">The input string.</param>
    /// <param name="charIndex">The 0-based char index.</param>
    /// <returns>The 0-based Text Element index.</returns>
    public static int GetTextElementIndexFromCharIndex(string text, int charIndex)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (charIndex < 0) throw new ArgumentOutOfRangeException(nameof(charIndex));
        if (charIndex >= text.Length) return CountTextElements(text); // End

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        int elementIndex = 0;
        
        while (enumerator.MoveNext())
        {
            int start = enumerator.ElementIndex;
            int length = StringInfo.GetNextTextElementLength(text, start);
            
            // If charIndex is within this element [start, start + length)
            if (charIndex >= start && charIndex < start + length)
            {
                return elementIndex;
            }
            elementIndex++;
        }

        throw new IndexOutOfRangeException("Character index out of bounds.");
    }

    /// <summary>
    /// Maps a Text Element index to its starting UTF-16 char index.
    /// </summary>
    /// <param name="text">The input string.</param>
    /// <param name="elementIndex">The 0-based Text Element index.</param>
    /// <returns>The 0-based char index where the element starts.</returns>
    public static int GetCharIndexFromTextElementIndex(string text, int elementIndex)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (elementIndex < 0) throw new ArgumentOutOfRangeException(nameof(elementIndex));

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        int currentElement = 0;
        
        while (enumerator.MoveNext())
        {
            if (currentElement == elementIndex)
            {
                return enumerator.ElementIndex;
            }
            currentElement++;
        }

        // If we ran out of elements, return text length (end)
        if (elementIndex == currentElement)
        {
            return text.Length;
        }

        throw new IndexOutOfRangeException("Text Element index out of bounds.");
    }
}
