namespace HarfRust;

/// <summary>
/// OpenType feature for text shaping.
/// </summary>
public readonly struct Feature
{
    /// <summary>
    /// The feature tag as a 4-byte value.
    /// </summary>
    public readonly uint Tag;

    /// <summary>
    /// The feature value (0 = disabled, 1 = enabled, or other values).
    /// </summary>
    public readonly uint Value;

    /// <summary>
    /// The start index in the buffer to apply this feature.
    /// </summary>
    public readonly uint Start;

    /// <summary>
    /// The end index in the buffer to apply this feature (uint.MaxValue for end).
    /// </summary>
    public readonly uint End;

    /// <summary>
    /// Creates a feature from a 4-character tag string.
    /// </summary>
    public Feature(string tag, uint value, uint start = 0, uint end = uint.MaxValue)
    {
        if (tag == null || tag.Length != 4)
            throw new ArgumentException("Tag must be exactly 4 characters.", nameof(tag));

        Tag = ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | (uint)tag[3];
        Value = value;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a feature from a raw tag value.
    /// </summary>
    public Feature(uint tag, uint value, uint start = 0, uint end = uint.MaxValue)
    {
        Tag = tag;
        Value = value;
        Start = start;
        End = end;
    }

    /// <summary>Create a feature to control standard ligatures ('liga').</summary>
    public static Feature StandardLigatures(bool enable) => new("liga", enable ? 1u : 0u);

    /// <summary>Create a feature to control discretionary ligatures ('dlig').</summary>
    public static Feature DiscretionaryLigatures(bool enable) => new("dlig", enable ? 1u : 0u);

    /// <summary>Create a feature to control kerning ('kern').</summary>
    public static Feature Kerning(bool enable) => new("kern", enable ? 1u : 0u);

    /// <summary>Create a feature to enable small caps ('smcp').</summary>
    public static Feature SmallCaps(bool enable) => new("smcp", enable ? 1u : 0u);

    /// <summary>Create a feature to control a stylistic set ('ss01'-'ss20').</summary>
    public static Feature StylisticSet(int setNumber, bool enable)
    {
        if (setNumber < 1 || setNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(setNumber), "Stylistic set must be 1-20.");

        var tag = $"ss{setNumber:D2}";
        return new Feature(tag, enable ? 1u : 0u);
    }
}
