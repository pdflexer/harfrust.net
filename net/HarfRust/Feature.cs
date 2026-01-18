namespace HarfRust;

/// <summary>
/// Defines an OpenType feature to apply during shaping.
/// </summary>
public struct Feature
{
    /// <summary>
    /// The feature tag (e.g., "liga", "kern").
    /// </summary>
    public uint Tag { get; set; }

    /// <summary>
    /// The feature value (0 = disabled, 1 = enabled).
    /// </summary>
    public uint Value { get; set; }

    /// <summary>
    /// The start index of the buffer to apply this feature.
    /// </summary>
    public uint Start { get; set; }

    /// <summary>
    /// The end index of the buffer to apply this feature.
    /// </summary>
    public uint End { get; set; }

    /// <summary>
    /// Creates a new feature with the specified tag and value.
    /// Defaults to applying to the entire range.
    /// </summary>
    /// <param name="tag">The 4-character feature tag (e.g. "liga").</param>
    /// <param name="value">The value (1 for enabled, 0 for disabled).</param>
    public Feature(string tag, uint value = 1)
    {
        Tag = HarfRustBuffer.CreateScriptTag(tag);
        Value = value;
        Start = 0;
        End = uint.MaxValue;
    }

    /// <summary>
    /// Creates a new feature with the specified tag, value, and range.
    /// </summary>
    /// <param name="tag">The 4-character feature tag.</param>
    /// <param name="value">The value.</param>
    /// <param name="start">Start index.</param>
    /// <param name="end">End index.</param>
    public Feature(string tag, uint value, uint start, uint end)
    {
        Tag = HarfRustBuffer.CreateScriptTag(tag);
        Value = value;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a Standard Ligatures ("liga") feature.
    /// </summary>
    public static Feature StandardLigatures(bool enable) => new Feature("liga", enable ? 1u : 0u);

    /// <summary>
    /// Creates a Discretionary Ligatures ("dlig") feature.
    /// </summary>
    public static Feature DiscretionaryLigatures(bool enable) => new Feature("dlig", enable ? 1u : 0u);

    /// <summary>
    /// Creates a Kerning ("kern") feature.
    /// </summary>
    public static Feature Kerning(bool enable) => new Feature("kern", enable ? 1u : 0u);

    /// <summary>
    /// Creates a Small Caps ("smcp") feature.
    /// </summary>
    public static Feature SmallCaps(bool enable) => new Feature("smcp", enable ? 1u : 0u);

    /// <summary>
    /// Creates a Stylistic Set feature ("ss01"-"ss20").
    /// </summary>
    /// <param name="setIndex">The set index (1-20).</param>
    /// <param name="enable">Whether to enable the set.</param>
    public static Feature StylisticSet(int setIndex, bool enable)
    {
        if (setIndex < 1 || setIndex > 20)
            throw new ArgumentOutOfRangeException(nameof(setIndex), "Stylistic set index must be between 1 and 20.");
        
        return new Feature($"ss{setIndex:D2}", enable ? 1u : 0u);
    }
}
