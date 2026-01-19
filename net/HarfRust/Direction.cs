namespace HarfRust;

/// <summary>
/// Text direction for shaping.
/// </summary>
public enum Direction : uint
{
    /// <summary>Initial, unset direction.</summary>
    Invalid = 0,
    /// <summary>Left-to-right text.</summary>
    LeftToRight = 4,
    /// <summary>Right-to-left text.</summary>
    RightToLeft = 5,
    /// <summary>Top-to-bottom text.</summary>
    TopToBottom = 6,
    /// <summary>Bottom-to-top text.</summary>
    BottomToTop = 7,
}
